// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a raw SQL SELECT clause.
/// </summary>
/// <param name="RawSql">The raw SQL projection string.</param>
/// <param name="Parameters">Optional parameters referenced by <paramref name="RawSql"/>.</param>
/// <param name="IsDistinct">If <see langword="true"/>, emits the DISTINCT keyword.</param>
public sealed record RawSelectNode(string RawSql, object?[]? Parameters, bool IsDistinct) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(IsDistinct);
        fingerprinter.Contribute(RawSql);
    }
}
