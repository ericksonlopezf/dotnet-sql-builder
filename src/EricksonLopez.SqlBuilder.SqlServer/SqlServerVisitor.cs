// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.SqlServer;

[RequiresDynamicCode("SQL Server dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("SQL Server dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
internal class SqlServerVisitor : SqlCompilerVisitor
{
    private readonly SqlServerCompiler _compiler;

    public SqlServerVisitor(SqlServerCompiler compiler, CompilationContext context) : base(compiler, context)
    {
        _compiler = compiler;
    }

    /// <inheritdoc />
    public override void Visit(OnConflictNode node)
    {
        throw new NotSupportedException(
            "SQL Server does not support ON CONFLICT syntax. " +
            "Use Sql.Raw() with a MERGE statement instead.");
    }

    /// <inheritdoc />
    public override void Visit(ReturningNode node)
    {
        Context.Sql.Append("OUTPUT ");
        if (node.Columns.Length == 0)
        {
            Context.Sql.Append("INSERTED.*");
        }
        else
        {
            for (int i = 0; i < node.Columns.Length; i++)
            {
                if (i > 0)
                {
                    Context.Sql.Append(", ");
                }

                Context.Sql.Append("INSERTED.");
                Context.Sql.Append(_compiler.Escape(node.Columns[i]));
            }
        }
        Context.Sql.Append(" ");
    }

    /// <summary>
    /// SQL Server does not natively support NULLS FIRST / NULLS LAST syntax.
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
    public override void Visit(WindowFunctionNode node)
    {
        if (node.FilterExpression != null || !string.IsNullOrEmpty(node.FilterRaw))
        {
            throw new NotSupportedException("SQL Server does not support the FILTER (WHERE ...) clause on window functions. Use conditional aggregation with CASE expressions or Sql.Raw() instead.");
        }

        base.Visit(node);
    }
}
