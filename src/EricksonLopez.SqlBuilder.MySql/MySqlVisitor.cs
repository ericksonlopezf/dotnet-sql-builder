// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.MySql;

/// <summary>
/// MySQL specific SQL visitor.
/// </summary>
[RequiresDynamicCode("MySQL dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("MySQL dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
internal class MySqlVisitor : SqlCompilerVisitor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlVisitor"/> class.
    /// </summary>
    /// <param name="compiler">The SQL compiler instance.</param>
    /// <param name="context">The compilation context containing the target SQL string builder and parameter collection.</param>
    public MySqlVisitor(MySqlCompiler compiler, CompilationContext context) : base(compiler, context) { }

    /// <inheritdoc />
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
            if (node.Nulls == NullsPosition.First)
            {
                Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"CASE WHEN {colName} IS NULL THEN 0 ELSE 1 END, ");
            }
            else if (node.Nulls == NullsPosition.Last)
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
    public override void Visit(WindowFunctionNode node)
    {
        if (node.FilterExpression != null || !string.IsNullOrEmpty(node.FilterRaw))
        {
            throw new NotSupportedException("MySQL does not support the FILTER (WHERE ...) clause on window functions. Use conditional aggregation with CASE expressions or Sql.Raw() instead.");
        }

        base.Visit(node);
    }

    /// <inheritdoc />
    public override void Visit(OnConflictNode node)
    {
        if (node.UpdateAction == "DO NOTHING")
        {
            Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"ON DUPLICATE KEY UPDATE {Escape("id")} = {Escape("id")} ");
            return;
        }

        Context.Sql.Append("ON DUPLICATE KEY UPDATE ");

        if (node.UpdateExpression != null)
        {
            if (node.UpdateExpression is System.Linq.Expressions.LambdaExpression lambda)
            {
                if (lambda.Body is System.Linq.Expressions.NewExpression newExpr && newExpr.Members != null)
                {
                    var assignments = newExpr.Members
                        .Select(m => Escape(SqlNamingHelper.ToSnakeCase(m.Name)))
                        .Select(col => $"{col} = VALUES({col})");
                    Context.Sql.Append(string.Join(", ", assignments));
                    return;
                }
                if (lambda.Body is System.Linq.Expressions.MemberExpression memberExpr)
                {
                    var col = Escape(SqlNamingHelper.ToSnakeCase(memberExpr.Member.Name));
                    Context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"{col} = VALUES({col})");
                    return;
                }
                throw new NotSupportedException("Unsupported lambda expression in ON DUPLICATE KEY UPDATE.");
            }
        }

        if (!string.IsNullOrEmpty(node.UpdateAction))
        {
            // Just append raw, parsing parameters
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

    /// <inheritdoc />
    public override void Visit(ReturningNode node)
    {
        throw new NotSupportedException("RETURNING clause is not natively supported in MySQL 8.x. Use LAST_INSERT_ID() for INSERT, or execute a SELECT after your DML statement. If you are using MariaDB 10.5+, use Sql.Raw() with RETURNING.");
    }
}
