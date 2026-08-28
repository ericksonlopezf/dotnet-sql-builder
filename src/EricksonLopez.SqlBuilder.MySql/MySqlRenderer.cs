// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

namespace EricksonLopez.SqlBuilder.MySql;

/// <summary>
/// Provides AOT-optimized SQL rendering for MySQL.
/// </summary>
public class MySqlRenderer : AotSqlRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlRenderer"/> class.
    /// </summary>
    /// <param name="compiler">The SQL compiler associated with this renderer.</param>
    public MySqlRenderer(ISqlCompiler compiler) : base(compiler)
    {
    }

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert for MySQL should use MySqlBatchStrategy via EricksonLopez.SqlBuilder.MySql.Bulk.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpdate<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Update is not natively implemented for MySQL.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkMerge<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Merge is not supported for MySQL (use OnConflict).");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Upsert is not yet implemented for MySQL.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsertIgnore<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert Ignore is not yet implemented for MySQL.");
}


