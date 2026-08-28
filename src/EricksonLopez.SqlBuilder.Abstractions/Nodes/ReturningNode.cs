// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a RETURNING clause used to retrieve column values from mutated rows.
/// </summary>
/// <param name="Columns">The names of the columns to return.</param>
public sealed record ReturningNode(string[] Columns) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        foreach (var col in Columns) fingerprinter.Contribute(col);
    }
}
