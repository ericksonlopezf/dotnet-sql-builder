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

/// <summary>
/// Provides PostgreSQL-specific SQL compilation and dialect rules.
/// </summary>
[RequiresDynamicCode("PostgreSQL dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("PostgreSQL dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
public class PostgreSqlCompiler : SqlCompilerBase
{
    private readonly ISqlRenderer _aotRenderer;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlCompiler"/> class.
    /// </summary>
    public PostgreSqlCompiler()
    {
        _aotRenderer = new PostgreSqlRenderer(this);
    }
    
    /// <inheritdoc />
    protected override ISqlRenderer AotRenderer => _aotRenderer;

    internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new PostgreSqlVisitor(this, context);

    internal override bool CompileBeforeSelect(SqlNodePartition partition, ISqlVisitor visitor, CompilationContext context)
    {
        if (partition.ExtensionNodes.Count == 0) return false;

        var copyNode = partition.ExtensionNodes.OfType<EricksonLopez.SqlBuilder.PostgreSql.CopyNode>().FirstOrDefault();

        if (copyNode != null)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"COPY {Escape(copyNode.TableName)} (");
            context.Sql.Append(string.Join(", ", copyNode.Columns.Select(Escape)));
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $") FROM {copyNode.FromSource} ");
            if (!string.IsNullOrEmpty(copyNode.Format))
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"WITH (FORMAT {copyNode.Format}) ");
            }
            return true;
        }
        return false;
    }

    internal override void CompileDistinct(SqlNodePartition partition, ISqlVisitor visitor, CompilationContext context)
    {
        partition.DistinctOnNode?.Accept(visitor);
    }

    internal override void CompileFrom(SqlNodePartition partition, ISqlVisitor visitor, CompilationContext context)
    {
        var fromNode = partition.FromNode;
        if (fromNode != null && !(fromNode is UnnestNode))
        {
            fromNode.Accept(visitor);
        }

        var unnestNodes = partition.UnnestNodes;
        for (int i = 0; i < unnestNodes.Count; i++)
        {
            var unnest = unnestNodes[i];
            if (fromNode != null && !(fromNode is UnnestNode) || i > 0)
            {
                context.Sql.Append(", UNNEST(");
            }
            else
            {
                context.Sql.Append("FROM UNNEST(");
            }

            var unnestParams = new List<string>();
            foreach (var arr in unnest.Arrays)
            {
                unnestParams.Add(context.Parameters.Add(arr));
            }
            context.Sql.Append(string.Join(", ", unnestParams)).Append(") AS ").Append(Escape(unnest.Alias)).Append(" ");
        }
    }

    /// <inheritdoc />
    public override string EscapeIdentifier(string identifier) => $"\"{identifier}\"";
    
    /// <inheritdoc />
    public override void EscapeIdentifier(StringBuilder sb, ReadOnlySpan<char> identifier)
    {
        sb.Append('"');
        sb.Append(identifier);
        sb.Append('"');
    }

    internal override void CompileDelete(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var deleteNode = partition.DeleteNode;
        if (deleteNode != null)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"DELETE FROM {Escape(deleteNode.TableName)} ");
        }

        if (partition.FromNode != null)
        {
            context.Sql.Append("USING ");
            var f = (FromNode)partition.FromNode;
            context.Sql.Append(Escape(f.TableName));
            if (!string.IsNullOrEmpty(f.Alias))
            {
                context.Sql.Append(" AS ").Append(Escape(f.Alias));
            }

            context.Sql.Append(" ");
        }

        foreach (var join in partition.JoinNodes)
        {
            join.Accept(visitor);
        }

        CompileWheres(partition.WhereNodes, visitor, context);
        partition.ReturningNode?.Accept(visitor);
    }
}
