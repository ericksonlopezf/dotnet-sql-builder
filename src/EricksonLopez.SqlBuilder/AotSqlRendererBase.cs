// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Provides a base implementation for an AOT-compatible SQL renderer used to generate fast, allocation-free SQL statements for single entity and bulk operations.
/// </summary>
public abstract class AotSqlRendererBase : ISqlRenderer
{
    /// <summary>
    /// Gets the SQL compiler used for dialect-specific generation.
    /// </summary>
    protected ISqlCompiler Compiler { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AotSqlRendererBase"/> class with the specified SQL compiler.
    /// </summary>
    /// <param name="compiler">The SQL compiler to delegate identifier escaping and parameter management to.</param>
    /// <exception cref="ArgumentNullException"><paramref name="compiler"/> is <see langword="null"/></exception>
    public AotSqlRendererBase(ISqlCompiler compiler)
    {
        Compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
    }

    /// <summary>
    /// Escapes the specified database identifier according to the current SQL dialect.
    /// </summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The escaped identifier string.</returns>
    protected string Escape(string identifier) => Compiler.Escape(identifier);

    /// <inheritdoc />
    public virtual SqlResult RenderInsert<T>(T entity, Span<bool> insertMask) where T : IStaticEntityMetadata<T>
    {
        using var activity = SqlBuilderDiagnostics.ActivitySource.StartActivity("SqlRenderer.RenderInsert");
        using var context = new CompilationContext(Compiler.CreateParameterManager());

        context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"INSERT INTO {Escape(T.TableName)} (");

        bool firstCol = true;
        for (int i = 0; i < insertMask.Length; i++)
        {
            if (insertMask[i])
            {
                if (!firstCol)
                {
                    context.Sql.Append(", ");
                }

                context.Sql.Append(Escape(T.GetColumnName(i)));
                firstCol = false;
            }
        }
        context.Sql.Append(")");
        AppendInsertOutputClause(context);
        context.Sql.Append(" VALUES (");

        bool firstVal = true;
        for (int i = 0; i < insertMask.Length; i++)
        {
            if (insertMask[i])
            {
                if (!firstVal)
                {
                    context.Sql.Append(", ");
                }

                context.Sql.Append(T.BindParameter(entity, i, context.Parameters));
                firstVal = false;
            }
        }

        context.Sql.Append(")");
        AppendInsertReturningClause(context);

        var sqlStr = context.Sql.ToString();
        var result = new SqlResult(sqlStr, context.Parameters.GetParameters());
        activity?.SetTag("db.statement", sqlStr);
        activity?.SetTag("sqlbuilder.query_type", "INSERT_AOT");
        return result;
    }

    /// <inheritdoc />
    public virtual SqlResult RenderUpdate<T>(T entity, Span<bool> setMask, Span<bool> whereMask) where T : IStaticEntityMetadata<T>
    {
        using var activity = SqlBuilderDiagnostics.ActivitySource.StartActivity("SqlRenderer.RenderUpdate");
        using var context = new CompilationContext(Compiler.CreateParameterManager());

        context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"UPDATE {Escape(T.TableName)} SET ");

        bool firstSet = true;
        for (int i = 0; i < setMask.Length; i++)
        {
            if (setMask[i])
            {
                if (!firstSet)
                {
                    context.Sql.Append(", ");
                }

                context.Sql.Append(Escape(T.GetColumnName(i)));
                context.Sql.Append(" = ");
                context.Sql.Append(T.BindParameter(entity, i, context.Parameters));
                firstSet = false;
            }
        }

        AppendUpdateOutputClause(context);
        
        context.Sql.Append(" WHERE ");

        bool firstWhere = true;
        for (int i = 0; i < whereMask.Length; i++)
        {
            if (whereMask[i])
            {
                if (!firstWhere)
                {
                    context.Sql.Append(" AND ");
                }

                context.Sql.Append(Escape(T.GetColumnName(i)));
                context.Sql.Append(" = ");
                context.Sql.Append(T.BindParameter(entity, i, context.Parameters));
                firstWhere = false;
            }
        }
        
        AppendUpdateReturningClause(context);

        var sqlStr = context.Sql.ToString();
        var result = new SqlResult(sqlStr, context.Parameters.GetParameters());
        activity?.SetTag("db.statement", sqlStr);
        activity?.SetTag("sqlbuilder.query_type", "UPDATE_AOT");
        return result;
    }

    /// <inheritdoc />
    public abstract BulkSqlResult RenderBulkInsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>;

    /// <inheritdoc />
    public abstract BulkSqlResult RenderBulkUpdate<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>;

    /// <inheritdoc />
    public abstract BulkSqlResult RenderBulkMerge<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>;

    /// <inheritdoc />
    public abstract BulkSqlResult RenderBulkUpsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>;

    /// <inheritdoc />
    public abstract BulkSqlResult RenderBulkInsertIgnore<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>;
    
    internal virtual void AppendInsertReturningClause(CompilationContext context) { }
    internal virtual void AppendUpdateReturningClause(CompilationContext context) { }
    internal virtual void AppendInsertOutputClause(CompilationContext context) { }
    internal virtual void AppendUpdateOutputClause(CompilationContext context) { }
}


