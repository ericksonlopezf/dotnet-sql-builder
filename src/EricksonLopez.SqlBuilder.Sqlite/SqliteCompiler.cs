// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.Sqlite;

/// <summary>
/// Provides SQLite specific compilation and dialect rules.
/// </summary>
[RequiresDynamicCode("SQLite dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("SQLite dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
public class SqliteCompiler : SqlCompilerBase
{
    /// <inheritdoc />
    public override string EscapeIdentifier(string identifier) => $"\"{identifier}\"";
    
    /// <inheritdoc />
    public override void EscapeIdentifier(StringBuilder sb, ReadOnlySpan<char> identifier)
    {
        sb.Append('"');
        sb.Append(identifier);
        sb.Append('"');
    }

    private readonly ISqlRenderer _aotRenderer;
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteCompiler"/> class.
    /// </summary>
    public SqliteCompiler() { _aotRenderer = new SqliteRenderer(this); }
    /// <inheritdoc />
    protected override ISqlRenderer AotRenderer => _aotRenderer;

    internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new SqliteVisitor(this, context);

    internal override void CompileLimitOffset(LimitOffsetNode? limitNode, ISqlVisitor visitor, CompilationContext context)
    {
        if (limitNode == null)
        {
            return;
        }

        var limit = limitNode.Limit;
        var offset = limitNode.Offset;

        if (limit.HasValue)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"LIMIT {limit.Value} ");
            if (offset.HasValue)
            {
                context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"OFFSET {offset.Value} ");
            }
        }
        else if (offset.HasValue)
        {
            context.Sql.Append(System.Globalization.CultureInfo.InvariantCulture, $"LIMIT -1 OFFSET {offset.Value} ");
        }
    }
}
