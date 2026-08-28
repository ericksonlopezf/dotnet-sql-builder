// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines an immutable SQL query composed of Abstract Syntax Tree (AST) nodes.
/// </summary>
public interface IAstQuery : ISqlQuery
{
    /// <summary>
    /// Gets the list of nodes that compose the SQL query.
    /// </summary>
    IReadOnlyList<ISqlNode> Nodes { get; }
}
