// Copyright © Erickson Lopez. MIT License.
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a HAVING clause constructed from a LINQ expression.
/// </summary>
/// <param name="Expression">The LINQ expression representing the HAVING predicate.</param>
/// <param name="IsOr">If <see langword="true"/>, combines this condition with OR; otherwise combines with AND.</param>
public sealed record ExpressionHavingNode(Expression Expression, bool IsOr = false) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(IsOr);
        
        var skeletonVisitor = new SkeletonExpressionVisitor();
        fingerprinter.Contribute(skeletonVisitor.GetSkeleton(Expression));
    }
}
