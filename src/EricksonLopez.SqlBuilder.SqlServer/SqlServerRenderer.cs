// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

namespace EricksonLopez.SqlBuilder.SqlServer;

/// <summary>
/// Provides AOT-optimized SQL rendering for SQL Server.
/// </summary>
public class SqlServerRenderer : AotSqlRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerRenderer"/> class.
    /// </summary>
    /// <param name="compiler">The compiler associated with this renderer.</param>
    public SqlServerRenderer(ISqlCompiler compiler) : base(compiler)
    {
    }

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert for SQL Server should use SqlBulkCopyStrategy via EricksonLopez.SqlBuilder.SqlServer.Bulk.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpdate<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Update is not natively supported for SQL Server via AotSqlRenderer.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkMerge<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Merge is not supported for SQL Server (see ADR-025).");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Upsert is not supported for SQL Server.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsertIgnore<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new System.NotSupportedException("AOT Bulk Insert Ignore is not supported for SQL Server.");

    internal override void AppendInsertOutputClause(CompilationContext context)
    {
        context.Sql.Append(" OUTPUT INSERTED.*");
    }

    internal override void AppendUpdateOutputClause(CompilationContext context)
    {
        context.Sql.Append(" OUTPUT INSERTED.*");
    }
}


