// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a single WHEN ... THEN ... branch within a CASE expression.
/// </summary>
/// <param name="WhenSql">The raw SQL condition for the WHEN clause (e.g., <c>"status = 1"</c>).</param>
/// <param name="WhenParameters">Optional parameters for the WHEN condition.</param>
/// <param name="ThenSql">The raw SQL result expression for the THEN clause (e.g., <c>"'Active'"</c>).</param>
/// <param name="ThenParameters">Optional parameters for the THEN expression.</param>
public record CaseWhenBranch(
    string WhenSql,
    object?[]? WhenParameters,
    string ThenSql,
    object?[]? ThenParameters);
