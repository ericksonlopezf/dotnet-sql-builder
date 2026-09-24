// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.MySql;

namespace EricksonLopez.SqlBuilder.MariaDb;

/// <summary>
/// Represents a MariaDB-specific SQL visitor that extends the MySQL visitor with native MariaDB features.
/// </summary>
/// <remarks>
/// Key differences from <see cref="MySqlVisitor"/>:
/// <list type="bullet">
///   <item><description><c>RETURNING</c> clause is natively supported in MariaDB 10.5+.</description></item>
/// </list>
/// All other behavior (LIMIT/OFFSET, ON DUPLICATE KEY UPDATE, NULLS emulation, etc.)
/// is inherited from the MySQL visitor unchanged.
/// </remarks>
[RequiresDynamicCode("MariaDB dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("MariaDB dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
internal sealed class MariaDbVisitor : MySqlVisitor
{
    public MariaDbVisitor(MariaDbCompiler compiler, CompilationContext context)
        : base(compiler, context)
    {
    }

    /// <summary>
    /// Emits the native <c>RETURNING</c> clause for MariaDB 10.5+ on INSERT, UPDATE, and DELETE statements.
    /// </summary>
    /// <param name="node">The returning AST node containing column specifications.</param>
    /// <remarks>
    /// This overrides the MySQL behavior which throws <see cref="NotSupportedException"/>.
    /// Requires MariaDB 10.5.0 or later.
    /// </remarks>
    public override void Visit(ReturningNode node)
    {
        if (node.Columns == null || node.Columns.Length == 0)
        {
            Context.Sql.Append("RETURNING * ");
            return;
        }

        Context.Sql.Append("RETURNING ");
        bool first = true;
        foreach (var col in node.Columns)
        {
            if (!first)
            {
                Context.Sql.Append(", ");
            }

            Context.Sql.Append(Escape(col));
            first = false;
        }

        Context.Sql.Append(' ');
    }
}
