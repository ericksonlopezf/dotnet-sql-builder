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
using Microsoft.Data.SqlClient;

namespace EricksonLopez.SqlBuilder.SqlServer;

/// <summary>
/// Provides a high-performance native bulk MERGE (INSERT + UPDATE) strategy for SQL Server.
/// </summary>
/// <remarks>
/// <para>
/// The merge is implemented in two stages:
/// <list type="number">
///   <item>Bulk-copies the entities into a temporary staging table using <see cref="SqlBulkCopy"/>.</item>
///   <item>Executes a <c>MERGE INTO target USING staging ON (key) WHEN MATCHED THEN UPDATE ... WHEN NOT MATCHED THEN INSERT ...</c> statement.</item>
/// </list>
/// </para>
/// <para>
/// This approach is faster than row-by-row upsert and avoids the deadlocks associated with
/// <c>IF EXISTS ... UPDATE ... ELSE INSERT</c> patterns.
/// </para>
/// </remarks>
public static class SqlBulkMergeStrategy
{
    /// <summary>
    /// Bulk-merges a collection of entities into the SQL Server table associated with <typeparamref name="T"/>
    /// using a staging-table + MERGE pattern.
    /// </summary>
    /// <typeparam name="T">The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="connection">An open <see cref="SqlConnection"/>.</param>
    /// <param name="entities">The entities to merge.</param>
    /// <param name="options">Optional bulk options (batch size, timeout).</param>
    /// <param name="transaction">An optional <see cref="SqlTransaction"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>
    /// A <see cref="BulkInsertResult{T}"/> with the total number of rows affected (inserted + updated).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live SQL Server; covered by integration tests.")]
    public static async Task<BulkInsertResult<T>> BulkMergeAsync<T>(
        SqlConnection connection,
        IEnumerable<T> entities,
        BulkOptions? options = null,
        SqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : IStaticEntityMetadata<T>
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        options ??= BulkOptions.Default;
        var list = entities as IList<T> ?? new List<T>(entities);
        if (list.Count == 0) return BulkInsertResult<T>.WithoutIdentities(0);

        var tableName = T.TableName;
        var columns = T.GetColumns().ToArray(); // materialize before first await (CS4007)
        var stagingTable = $"#staging_{tableName}_{Guid.NewGuid():N}";

        // Step 1: Create staging table with same schema
        var createStagingSql = BuildCreateStagingTableSql(tableName, stagingTable);
        await ExecuteCommandAsync(connection, createStagingSql, transaction, options.TimeoutSeconds, cancellationToken)
            .ConfigureAwait(false);

        // Step 2: Bulk-copy entities into staging table
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = stagingTable,
            BatchSize = options.BatchSize,
            BulkCopyTimeout = options.TimeoutSeconds,
            EnableStreaming = true
        };

        using var reader = new EntityDataReader<T>(list);
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            bulkCopy.ColumnMappings.Add(col.Name, col.Name);
        }
        await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);

        // Step 3: Execute MERGE from staging into target
        var mergeSql = BuildMergeSql<T>(tableName, stagingTable, columns);
        int rowsAffected = await ExecuteNonQueryAsync(connection, mergeSql, transaction, options.TimeoutSeconds, cancellationToken)
            .ConfigureAwait(false);

        // Step 4: Drop staging table
        await ExecuteCommandAsync(connection, $"DROP TABLE IF EXISTS {stagingTable}", transaction, options.TimeoutSeconds, cancellationToken)
            .ConfigureAwait(false);

        return BulkInsertResult<T>.WithoutIdentities(rowsAffected);
    }

    /// <summary>
    /// Bulk-merges a collection of entities using a generic database connection.
    /// </summary>
    /// <typeparam name="T">The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="connection">An open database connection (must be a <see cref="SqlConnection"/>).</param>
    /// <param name="entities">The entities to merge.</param>
    /// <param name="options">Optional bulk options (batch size, timeout).</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="BulkInsertResult{T}"/> with the total number of rows affected.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="connection"/> is not a <see cref="SqlConnection"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live SQL Server; covered by integration tests.")]
    public static Task<BulkInsertResult<T>> BulkMergeAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        BulkOptions? options = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : IStaticEntityMetadata<T>
    {
        if (connection is not SqlConnection sqlConn)
            throw new InvalidOperationException($"{nameof(SqlBulkMergeStrategy)}.{nameof(BulkMergeAsync)} requires a {nameof(SqlConnection)}.");
        return BulkMergeAsync<T>(sqlConn, entities, options, transaction as SqlTransaction, cancellationToken);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    internal static string BuildCreateStagingTableSql(string targetTable, string stagingTable)
        => $"SELECT TOP 0 * INTO {stagingTable} FROM [{targetTable}]";

    internal static string BuildMergeSql<T>(
        string targetTable,
        string stagingTable,
        ReadOnlySpan<ColumnMetadata> columns)
        where T : IStaticEntityMetadata<T>
    {
        var sb = new StringBuilder(512);
        sb.Append("MERGE INTO [").Append(targetTable).Append("] AS target ");
        sb.Append("USING ").Append(stagingTable).AppendLine(" AS source");
        sb.Append("ON (");

        bool firstKey = true;
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (col.HasFlag(ColumnFlags.PrimaryKey))
            {
                if (!firstKey) sb.Append(" AND ");
                sb.Append("target.[").Append(col.Name).Append("] = source.[").Append(col.Name).Append(']');
                firstKey = false;
            }
        }

        sb.AppendLine(")");
        sb.AppendLine("WHEN MATCHED THEN UPDATE SET");
        bool firstSet = true;
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (!col.HasFlag(ColumnFlags.PrimaryKey) && !col.HasFlag(ColumnFlags.Identity))
            {
                if (!firstSet) sb.Append(',');
                sb.Append("    target.[").Append(col.Name).Append("] = source.[").Append(col.Name).AppendLine("]");
                firstSet = false;
            }
        }

        sb.AppendLine("WHEN NOT MATCHED BY TARGET THEN INSERT (");
        bool firstCol = true;
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (!col.HasFlag(ColumnFlags.Identity))
            {
                if (!firstCol) sb.Append(", ");
                sb.Append('[').Append(col.Name).Append(']');
                firstCol = false;
            }
        }

        sb.AppendLine(") VALUES (");
        firstCol = true;
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (!col.HasFlag(ColumnFlags.Identity))
            {
                if (!firstCol) sb.Append(", ");
                sb.Append("source.[").Append(col.Name).Append(']');
                firstCol = false;
            }
        }
        sb.Append(");");

        return sb.ToString();
    }

    [ExcludeFromCodeCoverage(Justification = "Requires live SQL Server; covered by integration tests.")]
    private static async Task ExecuteCommandAsync(
        SqlConnection connection, string sql, SqlTransaction? tx,
        int timeoutSecs, CancellationToken ct)
    {
        using var cmd = new SqlCommand(sql, connection, tx) { CommandTimeout = timeoutSecs };
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage(Justification = "Requires live SQL Server; covered by integration tests.")]
    private static async Task<int> ExecuteNonQueryAsync(
        SqlConnection connection, string sql, SqlTransaction? tx,
        int timeoutSecs, CancellationToken ct)
    {
        using var cmd = new SqlCommand(sql, connection, tx) { CommandTimeout = timeoutSecs };
        return await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}




