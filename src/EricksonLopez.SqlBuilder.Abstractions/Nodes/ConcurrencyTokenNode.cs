// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents an optimistic concurrency check appended to an UPDATE statement.
/// </summary>
/// <remarks>
/// <para>
/// This node generates an additional <c>AND column = @expectedValue</c> in the WHERE clause
/// and, for integer/long tokens, also generates <c>SET column = column + 1</c> automatically.
/// </para>
/// <para>
/// <strong>Pattern:</strong>
/// <code>
/// UPDATE users SET name = @name, version = version + 1
/// WHERE id = @id AND version = @expectedVersion
/// </code>
/// </para>
/// <para>
/// If the UPDATE affects 0 rows, a concurrency conflict has occurred.
/// </para>
/// </remarks>
/// <param name="ColumnName">The name of the concurrency token column (e.g., "version").</param>
/// <param name="ExpectedValue">The expected current value of the token before update.</param>
/// <param name="NewValue">
/// Optional explicit new value. If <see langword="null"/>, integer/long tokens auto-increment
/// (<c>column = column + 1</c>); other types require an explicit new value.
/// </param>
/// <param name="AutoIncrement">
/// When <see langword="true"/>, emits <c>SET column = column + 1</c> for integer tokens.
/// When <see langword="false"/>, uses <paramref name="NewValue"/> in the SET clause.
/// </param>
public sealed record ConcurrencyTokenNode(
    string ColumnName,
    object? ExpectedValue,
    object? NewValue = null,
    bool AutoIncrement = true
) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}




