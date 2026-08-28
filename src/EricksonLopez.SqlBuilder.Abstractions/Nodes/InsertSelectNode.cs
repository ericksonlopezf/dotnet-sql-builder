// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Abstractions;
using System;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents an INSERT INTO ... SELECT ... statement, which inserts rows derived from a SELECT query.
/// </summary>
/// <param name="TableName">The name of the target table.</param>
/// <param name="Columns">
/// Optional array of column names. When specified, generates INSERT INTO table (col1, col2, ...) SELECT ...
/// When null or empty, generates INSERT INTO table SELECT ...
/// </param>
/// <param name="SelectQuery">The SELECT query whose results are inserted into the target table.</param>
/// <remarks>
/// <para>
/// This node is the primary mechanism for INSERT INTO ... SELECT statements in the AST.
/// It is fully supported across all SQL dialects.
/// </para>
/// <para>
/// Example generated SQL:
/// <code>
/// INSERT INTO archive_orders ("id", "customer_id", "total")
/// SELECT "id", "customer_id", "total" FROM "orders" WHERE "status" = @p0
/// </code>
/// </para>
/// </remarks>
public sealed record InsertSelectNode(
    string TableName,
    string[]? Columns,
    ISqlQuery SelectQuery
) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);
}





