// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a standard SELECT clause specifying one or more column names.
/// </summary>
/// <param name="Columns">The column names to project.</param>
/// <param name="IsDistinct">If <see langword="true"/>, emits the DISTINCT keyword.</param>
public sealed record SelectNode(string[] Columns, bool IsDistinct) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(IsDistinct);
        foreach (var col in Columns) fingerprinter.Contribute(col);
    }
}
