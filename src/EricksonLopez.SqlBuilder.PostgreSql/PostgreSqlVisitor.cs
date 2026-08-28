// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.PostgreSql;

[RequiresDynamicCode("PostgreSQL dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("PostgreSQL dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
internal class PostgreSqlVisitor : SqlCompilerVisitor
{
    public PostgreSqlVisitor(PostgreSqlCompiler compiler, CompilationContext context) : base(compiler, context) { }

    /// <inheritdoc />
    public override void Visit(DistinctOnNode node)
    {
        Context.Sql.Append("DISTINCT ON (");
        Context.Sql.Append(string.Join(", ", node.Columns.Select(c => Escape(c))));
        Context.Sql.Append(") ");
    }

    /// <summary>
    /// PostgreSQL does not support SQL Server's APPLY operators.
    /// Maps <c>CROSS APPLY</c> → <c>CROSS JOIN LATERAL</c>
    /// and <c>OUTER APPLY</c> → <c>LEFT JOIN LATERAL</c>.
    /// </summary>
    public override void Visit(SubqueryJoinNode node)
    {
        var joinType = node.Type;

        if (joinType == JoinType.CrossApply)
        {
            Context.Sql.Append("CROSS JOIN LATERAL (");
        }
        else if (joinType == JoinType.OuterApply)
        {
            Context.Sql.Append("LEFT JOIN LATERAL (");
        }
        else if (node.IsLateral)
        {
            string joinStr = joinType switch
            {
                JoinType.Inner => "INNER",
                JoinType.Left => "LEFT",
                JoinType.Right => "RIGHT",
                JoinType.Full => "FULL",
                JoinType.Cross => "CROSS",
                _ => joinType.ToString().ToUpperInvariant()
            };
            Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"{joinStr} JOIN LATERAL (");
        }
        else
        {
            // Fall back to base implementation for regular subquery joins
            base.Visit(node);
            return;
        }

        var sjResult = Compiler.Compile(node.Subquery, Context.Parameters);
        Context.Sql.Append(sjResult.Sql);
        Context.Sql.Append(") AS ").Append(Escape(node.Alias)).Append(" ");
        if (!string.IsNullOrEmpty(node.OnCondition))
        {
            Context.Sql.Append("ON ").Append(node.OnCondition).Append(" ");
        }
        else if (node.ExpressionCondition != null)
        {
            Context.Sql.Append("ON ");
            var parser = new SqlExpressionVisitor(Context.Sql, Context.Parameters, null);
            parser.Parse(node.ExpressionCondition);
            Context.Sql.Append(" ");
        }
    }

    /// <inheritdoc />
    public override void Visit(CteNode node)
    {
        Context.Sql.Append(Escape(node.Name));
        Context.Sql.Append(" AS ");
        if (node.Materialization == MaterializationHint.Materialized)
        {
            Context.Sql.Append("MATERIALIZED ");
        }
        else if (node.Materialization == MaterializationHint.NotMaterialized)
        {
            Context.Sql.Append("NOT MATERIALIZED ");
        }
        Context.Sql.Append("(");
        var cteResult = Compiler.Compile(node.Query, Context.Parameters);
        Context.Sql.Append(cteResult.Sql);
        Context.Sql.Append(")");
    }
}
