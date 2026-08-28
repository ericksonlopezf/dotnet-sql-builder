// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a scalar subquery projection in the SELECT clause (e.g. (SELECT COUNT(*) FROM ...) AS alias).
/// </summary>
/// <param name="Subquery">The subquery that computes the scalar value.</param>
/// <param name="Alias">The column alias assigned to the scalar result.</param>
public sealed record ScalarSubquerySelectNode(ISqlQuery Subquery, string Alias) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(Alias);
        if (Subquery is IAstQuery ast)
        {
            foreach (var node in ast.Nodes)
            {
                node.ContributeToFingerprint(fingerprinter);
            }
        }
        else if (Subquery != null)
        {
            fingerprinter.Contribute(Subquery.ToString() ?? "");
        }
    }
}
