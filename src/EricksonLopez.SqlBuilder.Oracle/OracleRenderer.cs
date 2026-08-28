// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

namespace EricksonLopez.SqlBuilder.Oracle;

/// <summary>
/// Provides AOT-optimized SQL rendering for Oracle Database.
/// </summary>
public class OracleRenderer : AotSqlRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleRenderer"/> class.
    /// </summary>
    /// <param name="compiler">The compiler associated with this renderer.</param>
    public OracleRenderer(ISqlCompiler compiler) : base(compiler)
    {
    }

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert is not yet implemented for Oracle.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpdate<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Update is not natively implemented for Oracle.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkMerge<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Merge is not supported for Oracle.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Upsert is not yet implemented for Oracle.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsertIgnore<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert Ignore is not yet implemented for Oracle.");
}


