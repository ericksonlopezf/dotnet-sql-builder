// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a standard FROM clause for a table or view.
/// </summary>
/// <param name="TableName">The name of the table or view.</param>
/// <param name="Alias">An optional alias for the table.</param>
public sealed record FromNode(string TableName, string? Alias = null) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(TableName);
        fingerprinter.Contribute(Alias);
    }
}
