// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Provides AOT-optimized SQL rendering for PostgreSQL, utilizing bulk UNNEST operations and specific syntax.
/// </summary>
public class PostgreSqlRenderer : AotSqlRendererBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlRenderer"/> class.
    /// </summary>
    /// <param name="compiler">The compiler associated with this renderer.</param>
    public PostgreSqlRenderer(ISqlCompiler compiler) : base(compiler)
    {
    }

    /// <inheritdoc />
    public override SqlResult RenderInsert<T>(T entity, Span<bool> insertMask)
    {
        using var activity = SqlBuilderDiagnostics.ActivitySource.StartActivity("SqlRenderer.RenderInsert_Postgres");
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

        context.Sql.Append(") VALUES (");

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

        context.Sql.Append(") RETURNING *");

        var sqlStr = context.Sql.ToString();
        return new SqlResult(sqlStr, context.Parameters.GetParameters());
    }

    /// <inheritdoc />
    public override SqlResult RenderUpdate<T>(T entity, Span<bool> setMask, Span<bool> whereMask)
    {
        using var activity = SqlBuilderDiagnostics.ActivitySource.StartActivity("SqlRenderer.RenderUpdate_Postgres");
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
        
        context.Sql.Append(" RETURNING *");

        var sqlStr = context.Sql.ToString();
        return new SqlResult(sqlStr, context.Parameters.GetParameters());
    }

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsert<T>(
        IEnumerable<T> entities, 
        List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, 
        int batchSize) 
    {
        using var activity = SqlBuilderDiagnostics.ActivitySource.StartActivity("SqlRenderer.RenderBulkInsert_Postgres_Unnest");
        
        var entitiesArray = entities switch
        {
            T[] arr => arr,
            _ => entities.ToArray()
        };
        if (entitiesArray.Length == 0)
        {
            throw new System.InvalidOperationException("Collection is empty.");
        }

        int colCount = T.ColumnCount;
        bool[] insertMaskArray = System.Buffers.ArrayPool<bool>.Shared.Rent(colCount);
        try
        {
            Span<bool> insertMask = new Span<bool>(insertMaskArray, 0, colCount);

            EricksonLopez.SqlBuilder.ColumnSelection.ColumnSelectionEngine<T>.SelectColumns(entitiesArray[0], SqlOperation.Insert, System.Runtime.InteropServices.CollectionsMarshal.AsSpan(rules), insertMask);

            var paramManager = Compiler.CreateParameterManager();
            T.ExtractColumnArrays(entitiesArray, insertMask, paramManager);

            using var context = new CompilationContext(paramManager);
            
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

            context.Sql.Append(") SELECT * FROM UNNEST(");

            bool firstVal = true;
            for (int i = 0; i < insertMask.Length; i++)
            {
                if (insertMask[i])
                {
                    if (!firstVal)
                    {
                        context.Sql.Append(", ");
                    }

                    context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"@C{i}"); 
                    firstVal = false;
                }
            }

            context.Sql.Append(")");

            var sqlResult = new SqlResult(context.Sql.ToString(), paramManager.GetParameters());
            return new BulkSqlResult(new[] { sqlResult });
        }
        // Stryker disable once all : ArrayPool buffer return optimization
        finally
        {
            System.Buffers.ArrayPool<bool>.Shared.Return(insertMaskArray);
        }
    }

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpdate<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new NotSupportedException("AOT Bulk Update is not natively implemented for PostgreSQL.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkMerge<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new NotSupportedException("AOT Bulk Merge is not supported for PostgreSQL (use OnConflict).");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkUpsert<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new NotSupportedException("AOT Bulk Upsert is not yet implemented for PostgreSQL.");

    /// <inheritdoc />
    public override BulkSqlResult RenderBulkInsertIgnore<T>(
        IEnumerable<T> entities, List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize)
        => throw new NotSupportedException("AOT Bulk Insert Ignore is not yet implemented for PostgreSQL.");
}

