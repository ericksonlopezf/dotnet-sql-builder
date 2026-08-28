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
/// Provides MySQL specific compilation and dialect rules.
/// </summary>
[RequiresDynamicCode("MySQL dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("MySQL dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
public class MySqlCompiler : SqlCompilerBase
{
    /// <inheritdoc />
    public override string EscapeIdentifier(string identifier) => $"`{identifier}`";
    
    /// <inheritdoc />
    public override void EscapeIdentifier(System.Text.StringBuilder sb, System.ReadOnlySpan<char> identifier)
    {
        sb.Append('`');
        sb.Append(identifier);
        sb.Append('`');
    }

    private readonly ISqlRenderer _aotRenderer;
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlCompiler"/> class.
    /// </summary>
    public MySqlCompiler() { _aotRenderer = CreateAotRenderer(); }

    /// <summary>
    /// Creates the AOT renderer for this compiler instance.
    /// Override in derived dialect compilers to provide a custom renderer.
    /// </summary>
    /// <remarks>
    /// Called once in the constructor after the instance is fully constructed.
    /// </remarks>
    /// <returns>A new <see cref="ISqlRenderer"/> instance for MySQL.</returns>
    protected virtual ISqlRenderer CreateAotRenderer() => new MySqlRenderer(this);

    /// <inheritdoc />
    protected override ISqlRenderer AotRenderer => _aotRenderer;

    internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new MySqlVisitor(this, context);

    internal override void CompileUpdate(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var updateNode = partition.UpdateNodes.OfType<UpdateNode>().LastOrDefault();
        if (updateNode != null)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"UPDATE {Escape(updateNode.TableName)} ");
        }

        foreach (var join in partition.JoinNodes)
        {
            join.Accept(visitor);
        }

        var setNodes = partition.SetNodes;
        var tokenNodes = partition.ConcurrencyTokenNodes;

        if (setNodes.Count > 0 || tokenNodes.Count > 0)
        {
            context.Sql.Append("SET ");
            bool firstSet = true;
            for (int i = 0; i < setNodes.Count; i++)
            {
                if (!firstSet) context.Sql.Append(", ");
                setNodes[i].Accept(visitor);
                firstSet = false;
            }

            for (int i = 0; i < tokenNodes.Count; i++)
            {
                if (!firstSet) context.Sql.Append(", ");
                var token = tokenNodes[i];
                if (token.AutoIncrement && token.NewValue == null)
                {
                    context.Sql.Append(Escape(token.ColumnName))
                               .Append(" = ")
                               .Append(Escape(token.ColumnName))
                               .Append(" + 1");
                }
                else
                {
                    var newParamName = context.Parameters.Add(token.NewValue);
                    context.Sql.Append(Escape(token.ColumnName))
                               .Append(" = ")
                               .Append(newParamName);
                }
                firstSet = false;
            }
            context.Sql.Append(" ");
        }

        CompileWheres(partition.WhereNodes, visitor, context);

        bool hasWhere = partition.WhereNodes.Count > 0;
        for (int i = 0; i < tokenNodes.Count; i++)
        {
            var token = tokenNodes[i];
            context.Sql.Append(hasWhere ? "AND " : "WHERE ");
            hasWhere = true;
            var paramName = context.Parameters.Add(token.ExpectedValue);
            context.Sql.Append(Escape(token.ColumnName))
                       .Append(" = ")
                       .Append(paramName)
                       .Append(" ");
        }
    }

    internal override void CompileDelete(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var deleteNode = partition.DeleteNode;
        var joinNodes = partition.JoinNodes;

        if (joinNodes.Count > 0 && deleteNode != null)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"DELETE {Escape(deleteNode.TableName)} FROM {Escape(deleteNode.TableName)} ");
            foreach (var join in joinNodes)
            {
                join.Accept(visitor);
            }
        }
        else if (deleteNode != null)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"DELETE FROM {Escape(deleteNode.TableName)} ");
        }

        CompileWheres(partition.WhereNodes, visitor, context);
    }

    internal override void CompileLimitOffset(LimitOffsetNode? limitNode, ISqlVisitor visitor, CompilationContext context)
    {
        if (limitNode == null)
        {
            return;
        }

        var limit = limitNode.Limit;
        var offset = limitNode.Offset;

        if (limit.HasValue)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"LIMIT {limit.Value} ");
            if (offset.HasValue)
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"OFFSET {offset.Value} ");
            }
        }
        else if (offset.HasValue)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"LIMIT 18446744073709551615 OFFSET {offset.Value} ");
        }
    }
}
