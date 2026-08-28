// Copyright © Erickson Lopez. MIT License.
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a secondary (or subsequent) sorting clause, typically matching a THEN BY operation.
/// </summary>
/// <param name="KeySelector">The LINQ expression identifying the secondary sort key member.</param>
/// <param name="IsDescending">If <see langword="true"/>, sorts in descending order; otherwise ascending.</param>
/// <param name="Nulls">Specifies the null ordering strategy.</param>
public sealed record ThenByNode(Expression KeySelector, bool IsDescending = false, NullsPosition Nulls = NullsPosition.None) : OrderByNode(KeySelector, IsDescending, Nulls)
{
    /// <inheritdoc />
    public override void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public override void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        base.ContributeToFingerprint(fingerprinter);
    }
}
