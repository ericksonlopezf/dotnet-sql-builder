// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a FROM clause containing a subquery.
/// </summary>
/// <param name="Query">The subquery used as the FROM data source.</param>
/// <param name="Alias">The alias assigned to the subquery.</param>
public sealed record SubqueryFromNode(ISqlQuery Query, string Alias) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        Query?.ContributeToFingerprint(fingerprinter);
        fingerprinter.Contribute(Alias);
    }
}
