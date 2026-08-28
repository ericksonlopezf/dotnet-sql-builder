// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a LIMIT and/or OFFSET clause for query pagination.
/// </summary>
/// <param name="Limit">The maximum number of rows to return. <see langword="null"/> means no limit is applied.</param>
/// <param name="Offset">The number of rows to skip before beginning to return rows. <see langword="null"/> means no offset is applied.</param>
public sealed record LimitOffsetNode(int? Limit, int? Offset) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(Limit.HasValue);
        fingerprinter.Contribute(Offset.HasValue);
    }
}







