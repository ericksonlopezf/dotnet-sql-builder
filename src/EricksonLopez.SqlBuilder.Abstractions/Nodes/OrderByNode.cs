// Copyright © Erickson Lopez. MIT License.
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a primary ORDER BY sorting clause based on a LINQ expression.
/// </summary>
/// <param name="KeySelector">The LINQ expression identifying the sort key member.</param>
/// <param name="IsDescending">If <see langword="true"/>, sorts in descending order; otherwise ascending.</param>
/// <param name="Nulls">Specifies the null ordering strategy.</param>
public record OrderByNode(Expression KeySelector, bool IsDescending = false, NullsPosition Nulls = NullsPosition.None) : ISqlNode
{
    /// <inheritdoc />
    public virtual void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public virtual void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(IsDescending);
        fingerprinter.Contribute((int)Nulls);
        
        var skeletonVisitor = new SkeletonExpressionVisitor();
        fingerprinter.Contribute(skeletonVisitor.GetSkeleton(KeySelector));
    }
}
