// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Builders.Bulk;
using Npgsql;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Provides a high-performance native bulk MERGE (INSERT + UPDATE) strategy for PostgreSQL
/// using <c>INSERT INTO ... ON CONFLICT DO UPDATE SET</c> (UPSERT).
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL natively supports upsert via the <c>ON CONFLICT DO UPDATE</c> clause,
/// which is atomically safe and significantly faster than manual merge patterns.
/// </para>
/// <para>
/// Entities are staged using the <see cref="NpgsqlCopyStrategy"/> binary COPY protocol
/// into a temporary table, then merged via a single <c>INSERT ... ON CONFLICT</c> statement.
/// </para>
/// </remarks>
public static class NpgsqlBulkMergeStrategy
{
    /// <summary>
    /// Bulk-merges a collection of entities into the PostgreSQL table associated with <typeparamref name="T"/>
    /// using <c>INSERT INTO ... ON CONFLICT (pk) DO UPDATE SET ...</c>.
    /// </summary>
    /// <typeparam name="T">The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="connection">An open <see cref="NpgsqlConnection"/>.</param>
    /// <param name="entities">The entities to merge.</param>
    /// <param name="options">Optional bulk options.</param>
    /// <param name="transaction">An optional <see cref="NpgsqlTransaction"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="BulkInsertResult{T}"/> with total rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live PostgreSQL; covered by integration tests.")]
    public static async Task<BulkInsertResult<T>> BulkMergeAsync<T>(
        NpgsqlConnection connection,
        IEnumerable<T> entities,
        BulkOptions? options = null,
        NpgsqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : IStaticEntityMetadata<T>
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        options ??= BulkOptions.Default;
        var list = entities as IList<T> ?? new List<T>(entities);
        if (list.Count == 0) return BulkInsertResult<T>.WithoutIdentities(0);

        var columns = T.GetColumns().ToArray(); // materialize before first await (CS4007)
        var tableName = T.TableName;
        var stagingTable = $"_staging_{tableName}_{Guid.NewGuid():N}";

        // Step 1: Create temp staging table
        var createStagingSql = $"CREATE TEMP TABLE \"{stagingTable}\" (LIKE \"{tableName}\" INCLUDING ALL)";
        await ExecuteAsync(connection, createStagingSql, transaction, cancellationToken).ConfigureAwait(false);

        // Step 2: COPY entities into staging table
        await NpgsqlCopyStrategy.BulkInsertAsync<T>(connection, list, options, transaction, cancellationToken)
            .ConfigureAwait(false);

        // Step 3: INSERT ... ON CONFLICT (pk) DO UPDATE SET ...
        var upsertSql = BuildUpsertSql<T>(tableName, stagingTable, columns);
        int rowsAffected = await ExecuteNonQueryAsync(connection, upsertSql, transaction, cancellationToken).ConfigureAwait(false);

        // Step 4: Drop staging table
        await ExecuteAsync(connection, $"DROP TABLE IF EXISTS \"{stagingTable}\"", transaction, cancellationToken).ConfigureAwait(false);

        return BulkInsertResult<T>.WithoutIdentities(rowsAffected);
    }

    /// <summary>
    /// Bulk-merges a collection of entities using a generic database connection.
    /// </summary>
    /// <typeparam name="T">The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="connection">An open database connection (must be an <see cref="NpgsqlConnection"/>).</param>
    /// <param name="entities">The entities to merge.</param>
    /// <param name="options">Optional bulk options.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="BulkInsertResult{T}"/> with total rows affected.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="connection"/> is not an instance of <see cref="NpgsqlConnection"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live PostgreSQL; covered by integration tests.")]
    public static Task<BulkInsertResult<T>> BulkMergeAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        BulkOptions? options = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : IStaticEntityMetadata<T>
    {
        if (connection is not NpgsqlConnection npgConn)
            throw new InvalidOperationException($"{nameof(NpgsqlBulkMergeStrategy)}.{nameof(BulkMergeAsync)} requires an {nameof(NpgsqlConnection)}.");
        return BulkMergeAsync<T>(npgConn, entities, options, transaction as NpgsqlTransaction, cancellationToken);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    internal static string BuildUpsertSql<T>(
        string targetTable,
        string stagingTable,
        ReadOnlySpan<ColumnMetadata> columns)
        where T : IStaticEntityMetadata<T>
    {
        var sb = new StringBuilder(512);
        sb.Append("INSERT INTO \"").Append(targetTable).AppendLine("\"");
        sb.Append("SELECT * FROM \"").Append(stagingTable).AppendLine("\"");
        sb.AppendLine("ON CONFLICT (");

        bool firstKey = true;
        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i].HasFlag(ColumnFlags.PrimaryKey))
            {
                if (!firstKey) sb.Append(", ");
                sb.Append('"').Append(columns[i].Name).Append('"');
                firstKey = false;
            }
        }

        sb.AppendLine(") DO UPDATE SET");
        bool firstSet = true;
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (!col.HasFlag(ColumnFlags.PrimaryKey) && !col.HasFlag(ColumnFlags.Identity))
            {
                if (!firstSet) sb.Append(',');
                sb.Append("    \"").Append(col.Name).Append("\" = EXCLUDED.\"").Append(col.Name).AppendLine("\"");
                firstSet = false;
            }
        }

        return sb.ToString();
    }

    [ExcludeFromCodeCoverage(Justification = "Requires live PostgreSQL; covered by integration tests.")]
    private static async Task ExecuteAsync(NpgsqlConnection conn, string sql, NpgsqlTransaction? tx, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage(Justification = "Requires live PostgreSQL; covered by integration tests.")]
    private static async Task<int> ExecuteNonQueryAsync(NpgsqlConnection conn, string sql, NpgsqlTransaction? tx, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}




