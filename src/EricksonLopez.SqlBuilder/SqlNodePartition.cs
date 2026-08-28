// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder;

internal sealed class SqlNodePartition
{
    private List<CteNode>? _cteNodes;
    /// <summary>Gets the collection of common table expression nodes.</summary>
    public IReadOnlyList<CteNode> CteNodes => _cteNodes ?? (IReadOnlyList<CteNode>)System.Array.Empty<CteNode>();
    /// <summary>Gets or sets the window page node.</summary>
    public WindowPageNode? WindowPageNode { get; set; }
    /// <summary>Gets or sets the distinct on node.</summary>
    public DistinctOnNode? DistinctOnNode { get; set; }
    
    private List<ISqlNode>? _selectNodes;
    /// <summary>Gets the collection of SELECT nodes.</summary>
    public IReadOnlyList<ISqlNode> SelectNodes => _selectNodes ?? (IReadOnlyList<ISqlNode>)System.Array.Empty<ISqlNode>();
    /// <summary>Gets or sets the FROM node.</summary>
    public ISqlNode? FromNode { get; set; }
    
    private List<ISqlNode>? _joinNodes;
    /// <summary>Gets the collection of JOIN nodes.</summary>
    public IReadOnlyList<ISqlNode> JoinNodes => _joinNodes ?? (IReadOnlyList<ISqlNode>)System.Array.Empty<ISqlNode>();
    
    private List<ISqlNode>? _whereNodes;
    /// <summary>Gets the collection of WHERE nodes.</summary>
    public IReadOnlyList<ISqlNode> WhereNodes => _whereNodes ?? (IReadOnlyList<ISqlNode>)System.Array.Empty<ISqlNode>();
    
    private List<GroupByNode>? _groupByNodes;
    /// <summary>Gets the collection of GROUP BY nodes.</summary>
    public IReadOnlyList<GroupByNode> GroupByNodes => _groupByNodes ?? (IReadOnlyList<GroupByNode>)System.Array.Empty<GroupByNode>();
    
    private List<ISqlNode>? _havingNodes;
    /// <summary>Gets the collection of HAVING nodes.</summary>
    public IReadOnlyList<ISqlNode> HavingNodes => _havingNodes ?? (IReadOnlyList<ISqlNode>)System.Array.Empty<ISqlNode>();
    
    private List<WindowNode>? _windowNodes;
    /// <summary>Gets the collection of WINDOW nodes.</summary>
    public IReadOnlyList<WindowNode> WindowNodes => _windowNodes ?? (IReadOnlyList<WindowNode>)System.Array.Empty<WindowNode>();
    
    private List<SetOperationNode>? _setOpNodes;
    /// <summary>Gets the collection of set operation nodes.</summary>
    public IReadOnlyList<SetOperationNode> SetOpNodes => _setOpNodes ?? (IReadOnlyList<SetOperationNode>)System.Array.Empty<SetOperationNode>();
    
    private List<ISqlNode>? _orderNodes;
    /// <summary>Gets the collection of order nodes.</summary>
    public IReadOnlyList<ISqlNode> OrderNodes => _orderNodes ?? (IReadOnlyList<ISqlNode>)System.Array.Empty<ISqlNode>();
    /// <summary>Gets or sets the LIMIT/OFFSET node.</summary>
    public LimitOffsetNode? LimitNode { get; set; }
    /// <summary>Gets or sets the query alias node.</summary>
    public QueryAliasNode? QueryAliasNode { get; set; }
    
    private List<ISqlNode>? _updateNodes;
    /// <summary>Gets the collection of UPDATE nodes.</summary>
    public IReadOnlyList<ISqlNode> UpdateNodes => _updateNodes ?? (IReadOnlyList<ISqlNode>)System.Array.Empty<ISqlNode>();
    
    private List<SetNode>? _setNodes;
    /// <summary>Gets the collection of SET nodes.</summary>
    public IReadOnlyList<SetNode> SetNodes => _setNodes ?? (IReadOnlyList<SetNode>)System.Array.Empty<SetNode>();
    /// <summary>Gets or sets the DELETE node.</summary>
    public DeleteNode? DeleteNode { get; set; }
    private List<ISqlNode>? _extensionNodes;
    /// <summary>Gets the collection of extension nodes.</summary>
    public IReadOnlyList<ISqlNode> ExtensionNodes => _extensionNodes ?? (IReadOnlyList<ISqlNode>)System.Array.Empty<ISqlNode>();
    /// <summary>Gets or sets the RETURNING node.</summary>
    public ReturningNode? ReturningNode { get; set; }

    private List<ConcurrencyTokenNode>? _concurrencyTokenNodes;
    /// <summary>Gets the collection of concurrency token nodes.</summary>
    public IReadOnlyList<ConcurrencyTokenNode> ConcurrencyTokenNodes => _concurrencyTokenNodes ?? (IReadOnlyList<ConcurrencyTokenNode>)System.Array.Empty<ConcurrencyTokenNode>();
    
    private List<UnnestNode>? _unnestNodes;
    /// <summary>Gets the collection of UNNEST nodes.</summary>
    public IReadOnlyList<UnnestNode> UnnestNodes => _unnestNodes ?? (IReadOnlyList<UnnestNode>)System.Array.Empty<UnnestNode>();
    
    /// <summary>Gets or sets the INSERT node.</summary>
    public InsertNode? InsertNode { get; set; }
    /// <summary>Gets or sets the VALUES node.</summary>
    public ValuesNode? ValuesNode { get; set; }
    /// <summary>Gets or sets the DEFAULT VALUES node.</summary>
    public DefaultValuesNode? DefaultValuesNode { get; set; }
    /// <summary>Gets or sets the ON CONFLICT node.</summary>
    public OnConflictNode? OnConflictNode { get; set; }

    // v0.8.0+ nodes
    /// <summary>Gets or sets the INSERT INTO ... SELECT node.</summary>
    public InsertSelectNode? InsertSelectNode { get; set; }
    private List<CompositeCursorNode>? _compositeCursorNodes;
    /// <summary>Gets the collection of composite cursor nodes.</summary>
    public IReadOnlyList<CompositeCursorNode> CompositeCursorNodes => _compositeCursorNodes ?? (IReadOnlyList<CompositeCursorNode>)System.Array.Empty<CompositeCursorNode>();

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlNodePartition"/> class.
    /// </summary>
    /// <param name="nodes">The nodes to partition.</param>
    public SqlNodePartition(IReadOnlyList<ISqlNode> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Classify(nodes[i]);
        }
    }

    private void Classify(ISqlNode node)
    {
        switch (node)
        {
            case WindowPageNode n: WindowPageNode = n; break;
            case WindowNode n: (_windowNodes ??= new()).Add(n); break;
            case ExpressionWhereNode n: (_whereNodes ??= new()).Add(n); break;
            case RawWhereNode n: (_whereNodes ??= new()).Add(n); break;
            case ExistsWhereNode n: (_whereNodes ??= new()).Add(n); break;
            case UpdateNode n: (_updateNodes ??= new()).Add(n); break;
            case SetNode n: (_setNodes ??= new()).Add(n); break;
            case DeleteNode n: DeleteNode = n; break;
            case ReturningNode n: ReturningNode = n; break;
            case CteNode n: (_cteNodes ??= new()).Add(n); break;
            case SubqueryFromNode n: FromNode = n; break;
            case FromNode n: FromNode = n; break;
            case SelectNode n: (_selectNodes ??= new()).Add(n); break;
            case ScalarSubquerySelectNode n: (_selectNodes ??= new()).Add(n); break;
            case JoinNode n: (_joinNodes ??= new()).Add(n); break;
            case GroupByNode n: (_groupByNodes ??= new()).Add(n); break;
            case SetOperationNode n: (_setOpNodes ??= new()).Add(n); break;
            case ThenByNode n: (_orderNodes ??= new()).Add(n); break;
            case OrderByNode n: (_orderNodes ??= new()).Add(n); break;
            case QueryAliasNode n: QueryAliasNode = n; break;
            case DistinctOnNode n: DistinctOnNode = n; break;
            case RawJoinNode n: (_joinNodes ??= new()).Add(n); break;
            case SubqueryJoinNode n: (_joinNodes ??= new()).Add(n); break;
            case InsertNode n: InsertNode = n; break;
            case ValuesNode n: ValuesNode = n; break;
            case ExpressionSelectNode n: (_selectNodes ??= new()).Add(n); break;
            case RawSelectNode n: (_selectNodes ??= new()).Add(n); break;
            case WindowFunctionNode n: (_selectNodes ??= new()).Add(n); break;
            case RawOrderByNode n: (_orderNodes ??= new()).Add(n); break;
            case OnConflictNode n: OnConflictNode = n; break;
            case DefaultValuesNode n: DefaultValuesNode = n; break;
            case ExpressionHavingNode n: (_havingNodes ??= new()).Add(n); break;
            case RawHavingNode n: (_havingNodes ??= new()).Add(n); break;
            case UnnestNode n: 
                (_unnestNodes ??= new()).Add(n);
                if (FromNode == null) FromNode = n;
                break;
            case LimitOffsetNode n:
                if (LimitNode == null) LimitNode = n;
                else LimitNode = new LimitOffsetNode(n.Limit ?? LimitNode.Limit, n.Offset ?? LimitNode.Offset);
                break;
            case ConcurrencyTokenNode n: (_concurrencyTokenNodes ??= new()).Add(n); break;
            case CaseNode n: (_selectNodes ??= new()).Add(n); break;
            case InsertSelectNode n: InsertSelectNode = n; break;
            case CompositeCursorNode n: (_compositeCursorNodes ??= new()).Add(n); break;
            default:
                (_extensionNodes ??= new()).Add(node);
                break;
        }
    }
}








