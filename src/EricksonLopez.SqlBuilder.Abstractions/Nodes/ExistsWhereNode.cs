// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a WHERE EXISTS (subquery) condition.
/// </summary>
/// <param name="Subquery">The subquery evaluated inside the EXISTS (or NOT EXISTS) predicate.</param>
/// <param name="IsNot">If <see langword="true"/>, generates NOT EXISTS; otherwise, generates EXISTS.</param>
/// <param name="IsOr">If <see langword="true"/>, combines this condition with OR instead of AND.</param>
public sealed record ExistsWhereNode(ISqlQuery Subquery, bool IsNot = false, bool IsOr = false) : ISqlNode
{
    /// <inheritdoc/>
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}




