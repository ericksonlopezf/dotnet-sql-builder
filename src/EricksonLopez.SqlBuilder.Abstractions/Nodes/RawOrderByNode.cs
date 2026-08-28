// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a raw SQL ORDER BY sorting clause.
/// </summary>
/// <param name="Condition">The raw SQL ORDER BY expression.</param>
/// <param name="IsDescending">If <see langword="true"/>, sorts in descending order; otherwise ascending.</param>
/// <param name="Parameters">Optional parameters referenced by <paramref name="Condition"/>.</param>
public sealed record RawOrderByNode(string Condition, bool IsDescending = false, object?[]? Parameters = null) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(IsDescending);
        fingerprinter.Contribute(Condition);
    }
}
