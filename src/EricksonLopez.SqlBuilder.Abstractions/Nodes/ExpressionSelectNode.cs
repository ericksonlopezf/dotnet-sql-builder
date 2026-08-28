// Copyright © Erickson Lopez. MIT License.
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a SELECT clause derived from a LINQ projection expression.
/// </summary>
/// <param name="Selector">The LINQ expression specifying projected members.</param>
/// <param name="IsDistinct">If <see langword="true"/>, emits the DISTINCT keyword.</param>
public sealed record ExpressionSelectNode(Expression Selector, bool IsDistinct) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(IsDistinct);
        var skeletonVisitor = new SkeletonExpressionVisitor();
        fingerprinter.Contribute(skeletonVisitor.GetSkeleton(Selector));
    }
}
