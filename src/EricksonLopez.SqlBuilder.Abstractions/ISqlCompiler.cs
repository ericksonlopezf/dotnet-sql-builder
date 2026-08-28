// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions;
using System;
using EricksonLopez.Result;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines the compiler that translates an <see cref="ISqlQuery"/> into a compiled <see cref="SqlResult"/>.
/// </summary>
public interface ISqlCompiler
{
    /// <summary>
    /// Compiles the specified SQL query into an executable result.
    /// </summary>
    /// <param name="query">The query to compile.</param>
    /// <returns>A <see cref="SqlResult"/> containing the generated SQL string and parameters.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
    SqlResult Compile(ISqlQuery query);
    
    /// <summary>
    /// Compiles the specified SQL query using an existing parameter manager.
    /// </summary>
    /// <param name="query">The query to compile.</param>
    /// <param name="existingParameters">An existing parameter manager to append parameters to.</param>
    /// <returns>A <see cref="SqlResult"/> containing the generated SQL string and parameters.</returns>
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
    SqlResult Compile(ISqlQuery query, IParameterManager? existingParameters);
    
    /// <summary>
    /// Escapes an identifier according to the specific database dialect rules.
    /// </summary>
    /// <param name="identifier">The identifier to escape.</param>
    /// <returns>The escaped identifier string.</returns>
    string Escape(string identifier);
    
    /// <summary>
    /// Escapes an identifier only if it contains special characters or keywords, according to dialect rules.
    /// </summary>
    /// <param name="identifier">The identifier to potentially escape.</param>
    /// <returns>The escaped identifier string.</returns>
    string EscapeIdentifier(string identifier);
    
    /// <summary>
    /// Creates a new parameter manager suitable for this compiler's dialect.
    /// </summary>
    /// <returns>A new <see cref="IParameterManager"/> instance.</returns>
    IParameterManager CreateParameterManager();

    /// <summary>
    /// Determines whether the compiler and target database provider support a specific capability.
    /// </summary>
    /// <param name="capability">The provider capability to check.</param>
    /// <returns><see langword="true"/> if the capability is supported; otherwise, <see langword="false"/>.</returns>
    bool SupportsCapability(ProviderCapability capability);
}



