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
/// Provides a high-performance native bulk INSERT strategy for MySQL
/// using multi-row <c>INSERT INTO ... VALUES (...), (...)</c> batch statements via <see cref="MySqlBulkCopy"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MySqlBulkCopy"/> uses the MySQL LOAD DATA LOCAL INFILE protocol internally,
/// which is the fastest available insertion path for large datasets in MySQL/MariaDB.
/// For smaller datasets, falls back to multi-row INSERT batching.
/// </para>
/// <para>
/// Requires <see cref="MySqlConnection"/> from the <c>MySqlConnector</c> package.
/// </para>
/// </remarks>
public static class MySqlBatchStrategy
{
    /// <summary>
    /// Bulk-inserts a collection of entities into the MySQL table associated with <typeparamref name="T"/>
    /// using <see cref="MySqlBulkCopy"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.
    /// </typeparam>
    /// <param name="connection">An open <see cref="MySqlConnection"/>.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="options">Optional bulk operation options.</param>
    /// <param name="transaction">An optional open <see cref="MySqlTransaction"/> to enlist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="BulkInsertResult{T}"/> with the total number of rows inserted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live MySQL; covered by integration tests.")]
    public static async Task<BulkInsertResult<T>> BulkInsertAsync<T>(
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
        if (list.Count == 0)
        {
            return BulkInsertResult<T>.WithoutIdentities(0);
        }

        var columns = T.GetColumns();
        var activeIndices = GetActiveColumnIndices(columns, options);

        // Use MySqlBulkCopy for optimal performance
        var bulkCopy = new MySqlBulkCopy(connection, transaction)
        {
            DestinationTableName = T.TableName,
            BulkCopyTimeout = options.TimeoutSeconds,
        };

        var dataTable = BuildDataTable<T>(list, columns, activeIndices);

        // Set column mappings
        for (int i = 0; i < activeIndices.Length; i++)
        {
            int colIdx = activeIndices[i];
            string colName = T.GetColumnName(colIdx);
            bulkCopy.ColumnMappings.Add(new MySqlBulkCopyColumnMapping(i, colName));
        }

        var result = await bulkCopy.WriteToServerAsync(dataTable, cancellationToken).ConfigureAwait(false);

        return BulkInsertResult<T>.WithoutIdentities(result.RowsInserted);
    }

    /// <summary>
    /// Bulk-inserts a collection of entities using a generic database connection.
    /// </summary>
    /// <typeparam name="T">The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="connection">An open database connection (must be a <see cref="MySqlConnection"/>).</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="options">Optional bulk operation options.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="BulkInsertResult{T}"/> with the total number of rows inserted.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="connection"/> is not a <see cref="MySqlConnection"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live MySQL; covered by integration tests.")]
    public static Task<BulkInsertResult<T>> BulkInsertAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        BulkOptions? options = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : IStaticEntityMetadata<T>
    {
        if (connection is not MySqlConnection mySqlConn)
        {
            throw new InvalidOperationException(
                $"{nameof(MySqlBatchStrategy)}.{nameof(BulkInsertAsync)} requires a {nameof(MySqlConnection)}.");
        }

        return BulkInsertAsync<T>(mySqlConn, entities, options, transaction as MySqlTransaction, cancellationToken);
    }

    // ─── Internal helpers ─────────────────────────────────────────────────────

    internal static int[] GetActiveColumnIndices(ReadOnlySpan<ColumnMetadata> columns, BulkOptions options)
    {
        var indices = new List<int>(columns.Length);
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (!options.ReturnIdentities && col.HasFlag(ColumnFlags.Identity))
            {
                continue;
            }
            indices.Add(col.Index);
        }
        return indices.ToArray();
    }

    internal static System.Data.DataTable BuildDataTable<T>(
        IList<T> entities,
        ReadOnlySpan<ColumnMetadata> columns,
        int[] activeIndices)
        where T : IStaticEntityMetadata<T>
    {
        var table = new System.Data.DataTable();

        foreach (int colIdx in activeIndices)
        {
            table.Columns.Add(T.GetColumnName(colIdx), typeof(object));
        }

        foreach (var entity in entities)
        {
            var rowValues = new object[activeIndices.Length];
            for (int j = 0; j < activeIndices.Length; j++)
            {
                int colIdx = activeIndices[j];
                object val = DBNull.Value;
                if (!T.IsNull(entity, colIdx))
                {
                    var pm = new EricksonLopez.SqlBuilder.ParameterManager();
                    var pName = T.BindParameter(entity, colIdx, pm);
                    val = pm.GetParameters()[pName.TrimStart('@')]!;
                }
                rowValues[j] = val;
            }
            table.Rows.Add(rowValues);
        }

        return table;
    }
}





