// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Provides a base implementation of <see cref="ISqlVisitor"/> where all virtual Visit methods are empty stubs.
/// Derived classes can override only the specific nodes they care about.
/// </summary>
public abstract class SqlVisitorBase : ISqlVisitor
{
    /// <inheritdoc/>
    public virtual void Visit(CteNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(DeleteNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(FromNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(SubqueryFromNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(UnnestNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(GroupByNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(ExpressionHavingNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(RawHavingNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(InsertNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(ValuesNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(ReturningNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(OnConflictNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(DefaultValuesNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(JoinNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(RawJoinNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(SubqueryJoinNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(LimitOffsetNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(ScalarSubquerySelectNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(OrderByNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(ThenByNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(RawOrderByNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(SelectNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(ExpressionSelectNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(QueryAliasNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(DistinctOnNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(RawSelectNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(SetOperationNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(UpdateNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(SetNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(ExpressionWhereNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(RawWhereNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(ExistsWhereNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(ConcurrencyTokenNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(WindowNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(WindowPageNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(WindowFunctionNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(CaseNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(InsertSelectNode node) { }
    /// <inheritdoc/>
    public virtual void Visit(CompositeCursorNode node) { }
    
    /// <inheritdoc />
    public virtual void VisitExtension(SqlExtensionNode node) => VisitUnknown(node);
    
    /// <inheritdoc />
    public virtual void VisitUnknown(ISqlNode node) => throw new System.NotSupportedException($"Node type {node?.GetType().Name ?? "null"} is not supported by {GetType().Name}.");
}




