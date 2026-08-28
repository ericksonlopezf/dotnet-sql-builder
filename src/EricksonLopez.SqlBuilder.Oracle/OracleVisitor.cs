// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.Oracle;

[RequiresDynamicCode("Oracle dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("Oracle dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
internal class OracleVisitor : SqlCompilerVisitor
{
    public OracleVisitor(OracleCompiler compiler, CompilationContext context) : base(compiler, context)
    {
    }

    /// <inheritdoc />
    public override void Visit(ReturningNode node)
    {
        if (node.Columns.Length == 0)
        {
            throw new NotSupportedException(
                "Oracle RETURNING clause requires explicit column names. " +
                "Use .Returning(\"col1\", \"col2\") instead of .Returning().");
        }

        Context.Sql.Append("RETURNING ");
        Context.Sql.Append(string.Join(", ", node.Columns.Select(c => Escape(c))));
        Context.Sql.Append(" INTO ");
        Context.Sql.Append(string.Join(", ", node.Columns.Select(c => $":out_{c.ToLowerInvariant()}")));
        Context.Sql.Append(" ");
    }

    /// <inheritdoc />
    public override void Visit(OnConflictNode node)
    {
        throw new NotSupportedException(
            "Oracle does not support ON CONFLICT syntax. " +
            "Use Sql.Raw() with a MERGE INTO statement instead.");
    }

    /// <inheritdoc />
    public override void Visit(WindowFunctionNode node)
    {
        if (node.FilterExpression != null || !string.IsNullOrEmpty(node.FilterRaw))
        {
            throw new NotSupportedException("Oracle does not support the FILTER (WHERE ...) clause on window functions. Use conditional aggregation with CASE expressions or Sql.Raw() instead.");
        }

        base.Visit(node);
    }
}
