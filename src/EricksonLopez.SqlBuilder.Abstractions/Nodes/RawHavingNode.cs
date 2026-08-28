// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a raw SQL HAVING clause.
/// </summary>
/// <param name="Condition">The raw SQL HAVING condition. Supports <c>{0}</c> parameter placeholders.</param>
/// <param name="Parameters">Optional parameters referenced by <paramref name="Condition"/>.</param>
/// <param name="IsOr">If <see langword="true"/>, combines this condition with OR; otherwise combines with AND.</param>
public sealed record RawHavingNode(string Condition, object?[]? Parameters = null, bool IsOr = false) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(IsOr);
        fingerprinter.Contribute(Condition);
    }
}
