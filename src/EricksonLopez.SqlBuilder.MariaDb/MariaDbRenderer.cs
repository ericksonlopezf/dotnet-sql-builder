// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;
using EricksonLopez.SqlBuilder.MySql;

namespace EricksonLopez.SqlBuilder.MariaDb;

/// <summary>
/// Provides AOT-optimized SQL rendering for MariaDB.
/// </summary>
/// <remarks>
/// Inherits from <see cref="MySqlRenderer"/>. MariaDB uses the same bulk operation
/// strategy as MySQL (via <c>MySqlConnector</c>), so all bulk methods delegate to
/// the MySQL renderer behavior.
/// </remarks>
public class MariaDbRenderer : MySqlRenderer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MariaDbRenderer"/> class.
    /// </summary>
    /// <param name="compiler">The MariaDB compiler instance.</param>
    public MariaDbRenderer(ISqlCompiler compiler) : base(compiler)
    {
    }

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert for MariaDB should use MySqlBatchStrategy via EricksonLopez.SqlBuilder.MySql.Bulk (MySqlConnector is wire-compatible with MariaDB).");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpdate<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Update is not natively implemented for MariaDB.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkMerge<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Merge is not supported for MariaDB (use OnConflict).");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Upsert is not yet implemented for MariaDB.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsertIgnore<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert Ignore is not yet implemented for MariaDB.");
}
