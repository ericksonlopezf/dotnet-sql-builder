// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Represents the AST node for a PostgreSQL COPY FROM operation.
/// </summary>
/// <param name="TableName">The name of the target table.</param>
/// <param name="Columns">The array of column names being copied.</param>
/// <param name="FromSource">The source identifier (e.g., STDIN).</param>
/// <param name="Format">The serialization format (e.g., BINARY, CSV).</param>
public sealed record CopyNode(string TableName, string[] Columns, string FromSource, string Format) : SqlExtensionNode
{
    /// <inheritdoc />
    public override void Accept(ISqlVisitor visitor) => visitor.VisitExtension(this);
}
