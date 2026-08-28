// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a GROUP BY clause in a SQL query.
/// </summary>
/// <param name="Columns">The ordered list of column names to group by.</param>
/// <param name="Type">The grouping aggregation type (Standard, Rollup, Cube, GroupingSets).</param>
/// <param name="Sets">The list of column sets when <paramref name="Type"/> is <see cref="GroupByType.GroupingSets"/>.</param>
public sealed record GroupByNode(
    IReadOnlyList<string> Columns,
    GroupByType Type = GroupByType.Standard,
    IReadOnlyList<IReadOnlyList<string>>? Sets = null) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <summary>
    /// Contributes the structure of this node to a deterministic cryptographic fingerprint.
    /// </summary>
    /// <param name="fingerprinter">The fingerprinter instance.</param>
    public void ContributeToFingerprinter(IQueryFingerprinter fingerprinter) => ContributeToFingerprint(fingerprinter);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(Type.ToString());
        foreach (var col in Columns) fingerprinter.Contribute(col);
        if (Sets != null)
        {
            foreach (var set in Sets)
            {
                foreach (var col in set) fingerprinter.Contribute(col);
            }
        }
    }
}
