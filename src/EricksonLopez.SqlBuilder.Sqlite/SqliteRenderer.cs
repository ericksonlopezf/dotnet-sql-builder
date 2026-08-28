// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

namespace EricksonLopez.SqlBuilder.Sqlite;

/// <summary>
/// Provides AOT-optimized SQL rendering for SQLite.
/// </summary>
public class SqliteRenderer : AotSqlRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteRenderer"/> class.
    /// </summary>
    /// <param name="compiler">The compiler associated with this renderer.</param>
    public SqliteRenderer(ISqlCompiler compiler) : base(compiler)
    {
    }

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert is not supported natively for SQLite.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpdate<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Update is not supported natively for SQLite.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkMerge<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Merge is not supported for SQLite (use OnConflict).");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Upsert is not yet implemented for SQLite.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsertIgnore<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert Ignore is not yet implemented for SQLite.");

    internal override void AppendInsertReturningClause(CompilationContext context)
    {
        context.Sql.Append(" RETURNING *");
    }

    internal override void AppendUpdateReturningClause(CompilationContext context)
    {
        context.Sql.Append(" RETURNING *");
    }
}


