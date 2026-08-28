// Copyright © Erickson Lopez. MIT License.
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents an ON CONFLICT clause used for upsert operations.
/// </summary>
/// <param name="TargetColumns">The columns that form the conflict target constraint or index.</param>
/// <param name="UpdateAction">The optional raw DO UPDATE SET SQL clause.</param>
/// <param name="UpdateExpression">The optional LINQ expression specifying update assignments.</param>
/// <param name="Parameters">Optional parameters referenced in the conflict resolution clause.</param>
public sealed record OnConflictNode(string[] TargetColumns, string? UpdateAction = null, Expression? UpdateExpression = null, object?[]? Parameters = null) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}
