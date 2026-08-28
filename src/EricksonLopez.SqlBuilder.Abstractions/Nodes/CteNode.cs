// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a Common Table Expression (CTE) clause in a SQL query.
/// </summary>
/// <param name="Name">The name of the CTE referenced in the main query body.</param>
/// <param name="Query">The SQL query that defines the CTE body.</param>
/// <param name="IsRecursive">If <see langword="true"/>, emits a RECURSIVE modifier (supported in PostgreSQL and SQLite).</param>
/// <param name="Materialization">The materialization hint (e.g., MATERIALIZED, NOT MATERIALIZED in PostgreSQL).</param>
public sealed record CteNode(string Name, ISqlQuery Query, bool IsRecursive = false, MaterializationHint Materialization = MaterializationHint.Default) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}
