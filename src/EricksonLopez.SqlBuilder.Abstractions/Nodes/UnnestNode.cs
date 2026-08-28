// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a FROM clause that unnests an array (common in PostgreSQL).
/// </summary>
/// <param name="Arrays">The array objects to expand with UNNEST.</param>
/// <param name="Alias">The alias assigned to the expanded table.</param>
public sealed record UnnestNode(object[] Arrays, string Alias) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(Alias);
        fingerprinter.Contribute(Arrays.Length);
    }
}
