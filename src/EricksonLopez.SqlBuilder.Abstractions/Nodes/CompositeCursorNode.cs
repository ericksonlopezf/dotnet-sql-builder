// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a composite keyset cursor node for multi-column keyset pagination.
/// </summary>
public sealed record CompositeCursorNode(
    CursorKey[] Keys,
    bool IsAfter = true
) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}
