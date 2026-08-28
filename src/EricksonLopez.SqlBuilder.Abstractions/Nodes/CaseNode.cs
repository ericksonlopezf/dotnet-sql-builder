// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a SQL CASE expression node for use inside a SELECT or WHERE clause.
/// </summary>
/// <remarks>
/// <para>
/// Generates SQL of the form:
/// <code>
/// CASE WHEN condition1 THEN result1 [WHEN condition2 THEN result2 ...] [ELSE else_result] END AS alias
/// </code>
/// </para>
/// <para>
/// Use via the <c>SelectCase</c> method on <c>SelectQuery&lt;T&gt;</c> or
/// the <c>CaseExpressionBuilder</c> fluent builder for a type-safe API.
/// </para>
/// </remarks>
/// <param name="Branches">The branches of the CASE expression.</param>
/// <param name="ElseSql">The optional ELSE SQL expression.</param>
/// <param name="ElseParameters">The optional parameters for the ELSE SQL expression.</param>
/// <param name="Alias">The optional alias for the CASE expression.</param>
public sealed record CaseNode(
    CaseWhenBranch[] Branches,
    string? ElseSql,
    object?[]? ElseParameters,
    string? Alias
) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}
