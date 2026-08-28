// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Serves as the base class for dialect-specific or custom AST nodes, allowing extensions beyond the core SQL standard.
/// </summary>
public abstract record SqlExtensionNode : ISqlNode
{
    /// <inheritdoc/>
    public abstract void Accept(ISqlVisitor visitor);
}




