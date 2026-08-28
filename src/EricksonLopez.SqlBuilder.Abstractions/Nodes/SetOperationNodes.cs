// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Abstractions;
using System;
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a set operation (e.g., UNION, INTERSECT, EXCEPT) combining two queries.
/// </summary>
/// <param name="Operation">The SQL set operator keyword (e.g., <c>"UNION"</c>, <c>"UNION ALL"</c>, <c>"INTERSECT"</c>, <c>"EXCEPT"</c>).</param>
/// <param name="Query">The right-hand query to combine with the main query using the set operation.</param>
public sealed record SetOperationNode(string Operation, ISqlQuery Query) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}







