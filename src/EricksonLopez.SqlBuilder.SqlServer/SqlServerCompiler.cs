// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.SqlServer;

/// <summary>
/// Provides SQL Server (T-SQL) specific compilation and dialect rules.
/// </summary>
[RequiresDynamicCode("SQL Server dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("SQL Server dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
public class SqlServerCompiler : SqlCompilerBase
{
    /// <inheritdoc />
    public override string EscapeIdentifier(string identifier) => $"[{identifier}]";

    /// <inheritdoc />
    public override bool SupportsCapability(ProviderCapability capability)
    {
        if (capability == ProviderCapability.None) return true;
        
        var supported = ProviderCapability.Apply | ProviderCapability.Cte | ProviderCapability.WindowFunctions | ProviderCapability.Merge;
        return (capability & supported) == capability;
    }

    /// <inheritdoc />
    public override void EscapeIdentifier(StringBuilder sb, ReadOnlySpan<char> identifier)
    {
        sb.Append('[');
        sb.Append(identifier);
        sb.Append(']');
    }

    private readonly ISqlRenderer _aotRenderer;
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerCompiler"/> class.
    /// </summary>
    public SqlServerCompiler() { _aotRenderer = new SqlServerRenderer(this); }
    /// <inheritdoc />
    protected override ISqlRenderer AotRenderer => _aotRenderer;

    internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new SqlServerVisitor(this, context);
    
    internal override void CompileLimitOffset(LimitOffsetNode? limitNode, ISqlVisitor visitor, CompilationContext context)
    {
        if (limitNode != null)
        {
            var limit = limitNode.Limit;
            var offset = limitNode.Offset ?? 0;
            
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"OFFSET {offset} ROWS ");
            
            if (limit.HasValue)
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"FETCH NEXT {limit.Value} ROWS ONLY ");
            }
        }
    }
    
    internal override void CompileInsert(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);

        // INSERT INTO ... SELECT ... takes priority over VALUES-based insert
        if (partition.InsertSelectNode != null)
        {
            partition.InsertSelectNode.Accept(visitor);
            return;
        }

        if (partition.InsertNode != null)
        {
            partition.InsertNode.Accept(visitor);
        }

        if (partition.ReturningNode != null)
        {
            if (context.Sql.Length > 0 && context.Sql[context.Sql.Length - 1] != ' ')
            {
                context.Sql.Append(" ");
            }

            partition.ReturningNode.Accept(visitor);
        }
        
        if (partition.DefaultValuesNode != null)
        {
            partition.DefaultValuesNode.Accept(visitor);
        }
        else if (partition.ValuesNode != null)
        {
            partition.ValuesNode.Accept(visitor);
        }

        partition.OnConflictNode?.Accept(visitor);
    }
    
    internal override void CompileUpdate(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var updateNode = partition.UpdateNodes.OfType<UpdateNode>().LastOrDefault();
        if (updateNode != null)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"UPDATE {Escape(updateNode.TableName)} ");
        }
        
        var setNodes = partition.SetNodes;
        var tokenNodes = partition.ConcurrencyTokenNodes;

        if (setNodes.Any() || tokenNodes.Count > 0)
        {
            context.Sql.Append("SET ");
            bool firstSet = true;
            for (int i = 0; i < setNodes.Count; i++)
            {
                if (!firstSet) context.Sql.Append(", ");
                setNodes[i].Accept(visitor);
                firstSet = false;
            }

            // Concurrency tokens: SET column = column + 1 (int/long) or SET column = @newValue
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
        
        var returningNode = partition.ReturningNode;
        if (returningNode != null)
        {
            context.Sql.Append("OUTPUT ");
            if (returningNode.Columns.Length == 0)
            {
                context.Sql.Append("INSERTED.*");
            }
            else
            {
                for (int i = 0; i < returningNode.Columns.Length; i++)
                {
                    if (i > 0)
                    {
                        context.Sql.Append(", ");
                    }

                    context.Sql.Append("INSERTED.");
                    context.Sql.Append(Escape(returningNode.Columns[i]));
                }
            }
            context.Sql.Append(" ");
        }
        
        if (partition.FromNode != null)
        {
            partition.FromNode.Accept(visitor);
        }

        foreach (var join in partition.JoinNodes)
        {
            join.Accept(visitor);
        }

        CompileWheres(partition.WhereNodes, visitor, context);

        // Concurrency tokens: AND column = @expectedValue (appended after regular WHERE)
        bool hasWhere = partition.WhereNodes.Count > 0;
        for (int i = 0; i < tokenNodes.Count; i++)
        {
            var token = tokenNodes[i];
            context.Sql.Append(hasWhere ? "AND " : "WHERE ")
                       .Append(Escape(token.ColumnName))
                       .Append(" = ")
                       .Append(context.Parameters.Add(token.ExpectedValue))
                       .Append(" ");
            hasWhere = true;
        }
    }
    
    internal override void CompileDelete(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
    {
        var partition = new SqlNodePartition(nodes);
        var deleteNode = partition.DeleteNode;
        if (deleteNode != null)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"DELETE FROM {Escape(deleteNode.TableName)} ");
        }
        
        var returningNode = partition.ReturningNode;
        if (returningNode != null)
        {
            context.Sql.Append("OUTPUT ");
            if (returningNode.Columns.Length == 0)
            {
                context.Sql.Append("DELETED.*");
            }
            else
            {
                for (int i = 0; i < returningNode.Columns.Length; i++)
                {
                    if (i > 0)
                    {
                        context.Sql.Append(", ");
                    }

                    context.Sql.Append("DELETED.");
                    context.Sql.Append(Escape(returningNode.Columns[i]));
                }
            }
            context.Sql.Append(" ");
        }
        
        if (partition.FromNode != null)
        {
            partition.FromNode.Accept(visitor);
        }

        foreach (var join in partition.JoinNodes)
        {
            join.Accept(visitor);
        }

        CompileWheres(partition.WhereNodes, visitor, context);
    }

    /// <inheritdoc />
    public override IParameterManager CreateParameterManager() => new ParameterManager("@", 2100);
}
