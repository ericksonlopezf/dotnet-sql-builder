// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.Sqlite;

[RequiresDynamicCode("SQLite dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("SQLite dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
internal class SqliteVisitor : SqlCompilerVisitor
{
    public SqliteVisitor(SqliteCompiler compiler, CompilationContext context) : base(compiler, context)
    {
    }

    /// <summary>
    /// SQLite (pre-3.30) does not natively support NULLS FIRST / NULLS LAST syntax.
    /// Emulates NULLS FIRST as: CASE WHEN [col] IS NULL THEN 0 ELSE 1 END, [col] [ASC|DESC]
    /// Emulates NULLS LAST  as: CASE WHEN [col] IS NULL THEN 1 ELSE 0 END, [col] [ASC|DESC]
    /// </summary>
    public override void Visit(OrderByNode node)
    {
        string? colName = null;
        var lambdaOrder = node.KeySelector as System.Linq.Expressions.LambdaExpression;
        if (lambdaOrder != null)
        {
            var member = lambdaOrder.Body switch
            {
                System.Linq.Expressions.UnaryExpression { Operand: System.Linq.Expressions.MemberExpression m } => m,
                System.Linq.Expressions.MemberExpression m => m,
                _ => null
            };
            if (member != null)
            {
                var snakeOrder = SqlNamingHelper.ToSnakeCase(member.Member.Name);
                colName = Escape(snakeOrder);
            }
        }
        if (colName != null)
        {
            if (node.Nulls == EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition.First)
            {
                Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"CASE WHEN {colName} IS NULL THEN 0 ELSE 1 END, ");
            }
            else if (node.Nulls == EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition.Last)
            {
                Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"CASE WHEN {colName} IS NULL THEN 1 ELSE 0 END, ");
            }

            Context.Sql.Append(colName);
        }

        if (node.IsDescending)
        {
            Context.Sql.Append(" DESC");
        }
    }

    /// <inheritdoc />
    public override void Visit(GroupByNode node)
    {
        if (node.Type != GroupByType.Standard)
        {
            throw new NotSupportedException($"SQLite does not support {node.Type} analytical aggregations.");
        }

        base.Visit(node);
    }

    /// <inheritdoc />
    public override void Visit(WindowFunctionNode node)
    {
        if (node.FilterExpression != null || !string.IsNullOrEmpty(node.FilterRaw))
        {
            throw new NotSupportedException("SQLite does not support the FILTER (WHERE ...) clause on window functions. Use conditional aggregation with CASE expressions or Sql.Raw() instead.");
        }

        base.Visit(node);
    }

    /// <inheritdoc />
    public override void Visit(OnConflictNode node)
    {
        Context.Sql.Append("ON CONFLICT ");

        if (node.TargetColumns != null && node.TargetColumns.Length > 0)
        {
            Context.Sql.Append("(");
            Context.Sql.Append(string.Join(", ", node.TargetColumns.Select(c => Escape(c))));
            Context.Sql.Append(") ");
        }

        if (node.UpdateExpression is System.Linq.Expressions.LambdaExpression lambda)
        {
            Context.Sql.Append("DO UPDATE SET ");

            if (lambda.Body is System.Linq.Expressions.NewExpression newExpr && newExpr.Members != null)
            {
                var assignments = newExpr.Members
                    .Select(m => SqlNamingHelper.ToSnakeCase(m.Name))
                    .Select(col => $"{Escape(col)} = EXCLUDED.{Escape(col)}");
                Context.Sql.Append(string.Join(", ", assignments));
                return;
            }

            if (lambda.Body is System.Linq.Expressions.MemberExpression memberExpr)
            {
                var col = SqlNamingHelper.ToSnakeCase(memberExpr.Member.Name);
                Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"{Escape(col)} = EXCLUDED.{Escape(col)}");
                return;
            }
        }

        if (!string.IsNullOrEmpty(node.UpdateAction))
        {
            if (!node.UpdateAction!.StartsWith("DO ", StringComparison.OrdinalIgnoreCase))
            {
                Context.Sql.Append("DO UPDATE SET ");
            }

            Context.Sql.Append(node.UpdateAction);
            if (node.Parameters != null)
            {
                foreach (var param in node.Parameters)
                {
                    Context.Parameters.Add(param);
                }
            }
        }
    }
}
