// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a single column assignment in a SET clause of an UPDATE statement.
/// </summary>
/// <param name="Column">The name of the column to assign. Mutually exclusive with <paramref name="RawExpression"/>.</param>
/// <param name="Value">The value to assign to the column when <paramref name="Column"/> is specified.</param>
/// <param name="RawExpression">An optional raw SQL expression used instead of a named column and value (e.g., <c>"col = col + 1"</c>).</param>
/// <param name="Parameters">Optional parameters referenced by <paramref name="RawExpression"/>.</param>
public sealed record SetNode(string? Column, object? Value, string? RawExpression = null, object?[]? Parameters = null) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(Column);
        fingerprinter.Contribute(RawExpression);
    }
}
