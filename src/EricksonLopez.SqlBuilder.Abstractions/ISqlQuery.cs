// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines an immutable SQL query that can be compiled into an executable statement.
/// </summary>
public interface ISqlQuery
{
    /// <summary>
    /// Builds the SQL query using the specified compiler.
    /// </summary>
    /// <param name="compiler">The SQL compiler to use.</param>
    /// <returns>A <see cref="SqlResult"/> containing the generated SQL and parameters.</returns>
    [RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
    [RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
    SqlResult Build(ISqlCompiler compiler);

    /// <summary>
    /// Gets the optional telemetry/instrumentation tag associated with this query.
    /// </summary>
    string? Tag { get; }

    /// <summary>
    /// Contributes the structure of this query to a deterministic cryptographic fingerprint.
    /// </summary>
    /// <param name="fingerprinter">The fingerprinter instance.</param>
    void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
    }
}
