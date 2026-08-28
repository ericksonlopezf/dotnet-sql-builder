// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a WINDOW clause in a SQL query.
/// </summary>
/// <param name="Name">The window name.</param>
/// <param name="PartitionBy">An array of columns to partition by.</param>
/// <param name="OrderBy">An array of columns to order by (optionally containing ASC/DESC suffixes).</param>
public sealed record WindowNode(string Name, string[]? PartitionBy, string[]? OrderBy) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}








