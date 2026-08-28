// Copyright © Erickson Lopez. MIT License.
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.MySql;

namespace EricksonLopez.SqlBuilder.MariaDb;

/// <summary>
/// Provides MariaDB-specific compilation and dialect rules.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MariaDbCompiler"/> extends <see cref="MySqlCompiler"/> with native MariaDB features:
/// <list type="bullet">
///   <item><description>Native <c>RETURNING</c> clause support (MariaDB 10.5+).</description></item>
/// </list>
/// </para>
/// <para>
/// All MySQL-compatible features are inherited: backtick quoting, <c>LIMIT/OFFSET</c>,
/// <c>ON DUPLICATE KEY UPDATE</c>, multi-table DELETE/UPDATE with JOINs,
/// and <c>NULLS FIRST/LAST</c> emulation via <c>CASE WHEN</c>.
/// </para>
/// </remarks>
[RequiresDynamicCode("MariaDB dialect compiler uses dynamic code generation when evaluating LINQ expressions. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("MariaDB dialect compiler accesses member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
public class MariaDbCompiler : MySqlCompiler
{
    /// <inheritdoc />
    /// <remarks>
    /// Returns a <see cref="MariaDbRenderer"/> configured for this compiler instance.
    /// </remarks>
    protected override ISqlRenderer CreateAotRenderer() => new MariaDbRenderer(this);

    internal override SqlVisitorBase CreateVisitor(CompilationContext context)
        => new MariaDbVisitor(this, context);
}
