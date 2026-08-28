// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a window function expression in a SELECT clause (inline OVER clause).
/// </summary>
/// <remarks>
/// Generates SQL of the form:
/// <code>FUNC([Column]) OVER (PARTITION BY col1, col2 ORDER BY col3 DESC) AS alias</code>
/// </remarks>
/// <param name="FunctionName">
/// The SQL window function name. Examples: ROW_NUMBER, RANK, DENSE_RANK, LAG, LEAD, SUM, AVG, COUNT, MIN, MAX.
/// </param>
/// <param name="ColumnName">
/// Optional column argument. <see langword="null"/> for functions that take no argument (ROW_NUMBER, RANK, DENSE_RANK, COUNT(*)).
/// </param>
/// <param name="Offset">
/// Optional numeric offset argument for LAG/LEAD.
/// </param>
/// <param name="DefaultValue">
/// Optional default value for LAG/LEAD when the offset goes out of range.
/// </param>
/// <param name="PartitionByColumns">Columns for the PARTITION BY clause. Empty means no PARTITION BY.</param>
/// <param name="OrderByColumns">Column names for the ORDER BY clause inside OVER. Empty means no ORDER BY.</param>
/// <param name="OrderByDescending">Parallel array to <paramref name="OrderByColumns"/>. <see langword="true"/> = DESC.</param>
/// <param name="Alias">The AS alias for this expression in the SELECT list.</param>
/// <param name="FilterExpression">Optional LINQ predicate for the FILTER (WHERE ...) clause.</param>
/// <param name="FilterRaw">Optional raw SQL condition for the FILTER (WHERE ...) clause.</param>
/// <param name="FilterRawArgs">Optional arguments for <paramref name="FilterRaw"/>.</param>
public sealed record WindowFunctionNode(
    string FunctionName,
    string? ColumnName,
    int? Offset,
    object? DefaultValue,
    string[] PartitionByColumns,
    string[] OrderByColumns,
    bool[] OrderByDescending,
    string Alias,
    System.Linq.Expressions.Expression? FilterExpression = null,
    string? FilterRaw = null,
    object?[]? FilterRawArgs = null
) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}






