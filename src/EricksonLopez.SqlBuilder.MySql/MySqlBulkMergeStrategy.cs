// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Builders.Bulk;
using MySqlConnector;

namespace EricksonLopez.SqlBuilder.MySql;

/// <summary>
/// Provides a high-performance native bulk MERGE (INSERT + UPDATE) strategy for MySQL/MariaDB
/// using <c>INSERT INTO ... ON DUPLICATE KEY UPDATE</c>.
/// </summary>
/// <remarks>
/// <para>
/// MySQL and MariaDB support <c>ON DUPLICATE KEY UPDATE</c> as a native upsert mechanism.
/// This strategy builds multi-row INSERT statements with upsert semantics.
/// </para>
/// </remarks>
public static class MySqlBulkMergeStrategy
{
    /// <summary>
    /// Bulk-merges a collection of entities into the MySQL table associated with <typeparamref name="T"/>
    /// using <c>INSERT INTO ... VALUES (...) ON DUPLICATE KEY UPDATE ...</c>.
    /// </summary>
    /// <typeparam name="T">The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="connection">An open <see cref="MySqlConnection"/>.</param>
    /// <param name="entities">The entities to merge.</param>
    /// <param name="options">Optional bulk options (batch size, timeout).</param>
    /// <param name="transaction">An optional <see cref="MySqlTransaction"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="BulkInsertResult{T}"/> with total rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live MySQL; covered by integration tests.")]
    public static async Task<BulkInsertResult<T>> BulkMergeAsync<T>(
        MySqlConnection connection,
        IEnumerable<T> entities,
        BulkOptions? options = null,
        MySqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : IStaticEntityMetadata<T>
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        options ??= BulkOptions.Default;
        var list = entities as IList<T> ?? new List<T>(entities);
        if (list.Count == 0) return BulkInsertResult<T>.WithoutIdentities(0);

        var columns = T.GetColumns().ToArray(); // materialize before await boundary (CS4007)
        int batchSize = options.BatchSize > 0 ? options.BatchSize : 1000;
        int totalAffected = 0;

        // Process in batches to avoid MySQL max_allowed_packet limits
        for (int offset = 0; offset < list.Count; offset += batchSize)
        {
            int end = System.Math.Min(offset + batchSize, list.Count);
            var batch = list is List<T> l ? l.GetRange(offset, end - offset) : Slice(list, offset, end);

            var (sql, parameters) = BuildUpsertStatement<T>(batch, columns);
            await using var cmd = new MySqlCommand(sql, connection, transaction)
            {
                CommandTimeout = options.TimeoutSeconds
            };

            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            totalAffected += await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return BulkInsertResult<T>.WithoutIdentities(totalAffected);
    }

    /// <summary>
    /// Bulk-merges a collection of entities using a generic database connection.
    /// </summary>
    /// <typeparam name="T">The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="connection">An open database connection (must be a <see cref="MySqlConnection"/>).</param>
    /// <param name="entities">The entities to merge.</param>
    /// <param name="options">Optional bulk options (batch size, timeout).</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="BulkInsertResult{T}"/> with total rows affected.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="connection"/> is not a <see cref="MySqlConnection"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live MySQL; covered by integration tests.")]
    public static Task<BulkInsertResult<T>> BulkMergeAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        BulkOptions? options = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : IStaticEntityMetadata<T>
    {
        if (connection is not MySqlConnection mySqlConn)
            throw new InvalidOperationException($"{nameof(MySqlBulkMergeStrategy)}.{nameof(BulkMergeAsync)} requires a {nameof(MySqlConnection)}.");
        return BulkMergeAsync<T>(mySqlConn, entities, options, transaction as MySqlTransaction, cancellationToken);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    internal static (string Sql, List<(string Name, object? Value)> Parameters) BuildUpsertStatement<T>(
        IList<T> batch,
        ReadOnlySpan<ColumnMetadata> columns)
        where T : IStaticEntityMetadata<T>
    {
        var sb = new StringBuilder(512);
        var parameters = new List<(string, object?)>();

        sb.Append("INSERT INTO `").Append(T.TableName).AppendLine("` (");
        bool firstCol = true;
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (col.HasFlag(ColumnFlags.Identity)) continue;
            if (!firstCol) sb.Append(", ");
            sb.Append('`').Append(col.Name).Append('`');
            firstCol = false;
        }
        sb.AppendLine(") VALUES");

        for (int row = 0; row < batch.Count; row++)
        {
            var entity = batch[row];
            if (row > 0) sb.Append(',');
            sb.Append('(');
            bool firstVal = true;
            for (int i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                if (col.HasFlag(ColumnFlags.Identity)) continue;
                if (!firstVal) sb.Append(", ");

                var paramName = $"@p_{row}_{i}";
                sb.Append(paramName);

                object? value = null;
                if (!T.IsNull(entity, col.Index))
                {
                    var pm = new EricksonLopez.SqlBuilder.ParameterManager();
                    var pName = T.BindParameter(entity, col.Index, pm);
                    value = pm.GetParameters()[pName.TrimStart('@')];
                }
                parameters.Add((paramName, value));
                firstVal = false;
            }
            sb.AppendLine(")");
        }

        sb.AppendLine("ON DUPLICATE KEY UPDATE");
        bool firstSet = true;
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (col.HasFlag(ColumnFlags.PrimaryKey) || col.HasFlag(ColumnFlags.Identity)) continue;
            if (!firstSet) sb.Append(',');
            sb.Append('`').Append(col.Name).Append("` = VALUES(`").Append(col.Name).AppendLine("`)");
            firstSet = false;
        }

        return (sb.ToString(), parameters);
    }

    internal static List<T> Slice<T>(IList<T> source, int start, int end)
    {
        var result = new List<T>(end - start);
        for (int i = start; i < end; i++) result.Add(source[i]);
        return result;
    }
}





