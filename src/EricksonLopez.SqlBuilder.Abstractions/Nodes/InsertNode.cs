// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents the INTO part of an INSERT statement specifying target table and columns.
/// </summary>
/// <param name="TableName">The name of the target table to insert into.</param>
/// <param name="Columns">The list of column names receiving inserted values.</param>
public sealed record InsertNode(string TableName, IReadOnlyList<string> Columns) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(TableName);
        foreach (var col in Columns) fingerprinter.Contribute(col);
    }
}
