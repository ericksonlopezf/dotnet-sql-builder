// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a completely raw JOIN clause.
/// </summary>
/// <param name="JoinSql">The raw SQL string.</param>
/// <param name="Parameters">Optional parameters.</param>
public sealed record RawJoinNode(string JoinSql, object?[]? Parameters = null) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(JoinSql);
    }
}
