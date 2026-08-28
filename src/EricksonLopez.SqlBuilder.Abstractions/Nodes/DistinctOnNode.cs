// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a DISTINCT ON clause (typically used in PostgreSQL).
/// </summary>
/// <param name="Columns">The columns used for the DISTINCT ON expression.</param>
public sealed record DistinctOnNode(string[] Columns) : ISqlNode
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
