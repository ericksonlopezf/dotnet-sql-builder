// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents the VALUES clause of an INSERT statement containing multiple sets of values.
/// </summary>
/// <param name="ValuesSets">The nested collections of row value sets to insert.</param>
public sealed record ValuesNode(IReadOnlyList<IReadOnlyList<object?>> ValuesSets) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}
