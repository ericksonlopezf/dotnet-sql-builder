// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Builders;

/// <summary>
/// Provides a fluent builder for constructing SQL CASE expressions.
/// </summary>
/// <remarks>
/// <para>
/// Usage example:
/// <code>
/// var caseExpr = new CaseExpressionBuilder()
///     .When("status = {0}", 1).Then("'Active'")
///     .When("status = {0}", 2).Then("'Inactive'")
///     .Else("'Unknown'")
///     .As("status_label")
///     .Build();
///
/// var query = Sql.From&lt;User&gt;().SelectCase(caseExpr);
/// </code>
/// </para>
/// </remarks>
public sealed class CaseExpressionBuilder
{
    private readonly List<CaseWhenBranch> _branches = new();
    private string? _elseSql;
    private object?[]? _elseParameters;
    private string? _alias;

    // State for the pending WHEN
    private string? _pendingWhenSql;
    private object?[]? _pendingWhenParameters;

    /// <summary>
    /// Adds a WHEN condition to the CASE expression.
    /// </summary>
    /// <param name="whenSql">The SQL condition (supports <c>{0}</c> parameter placeholders).</param>
    /// <param name="parameters">Optional parameters for the condition.</param>
    /// <returns>This builder for fluent chaining. Call <see cref="Then"/> next.</returns>
    public CaseExpressionBuilder When(string whenSql, params object?[] parameters)
    {
        _pendingWhenSql = whenSql;
        _pendingWhenParameters = parameters.Length > 0 ? parameters : null;
        return this;
    }

    /// <summary>
    /// Adds a THEN result to the preceding WHEN condition.
    /// </summary>
    /// <param name="thenSql">The SQL result expression (e.g., <c>"'Active'"</c> or <c>"{0}"</c>).</param>
    /// <param name="parameters">Optional parameters for the result expression.</param>
    /// <returns>This builder for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException"><see cref="When(string, object?[])"/> was not called prior to <see cref="Then(string, object?[])"/></exception>
    public CaseExpressionBuilder Then(string thenSql, params object?[] parameters)
    {
        if (_pendingWhenSql == null)
        {
            throw new System.InvalidOperationException("Call When() before Then().");
        }

        _branches.Add(new CaseWhenBranch(
            _pendingWhenSql,
            _pendingWhenParameters,
            thenSql,
            parameters.Length > 0 ? parameters : null));

        _pendingWhenSql = null;
        _pendingWhenParameters = null;
        return this;
    }

    /// <summary>
    /// Sets the ELSE result of the CASE expression.
    /// </summary>
    /// <param name="elseSql">The SQL result for the ELSE clause.</param>
    /// <param name="parameters">Optional parameters for the ELSE result.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public CaseExpressionBuilder Else(string elseSql, params object?[] parameters)
    {
        _elseSql = elseSql;
        _elseParameters = parameters.Length > 0 ? parameters : null;
        return this;
    }

    /// <summary>
    /// Sets the column alias for the CASE expression result (AS alias).
    /// </summary>
    /// <param name="alias">The alias name.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public CaseExpressionBuilder As(string alias)
    {
        _alias = alias;
        return this;
    }

    /// <summary>
    /// Builds and returns the <see cref="CaseNode"/> AST node.
    /// </summary>
    /// <returns>A <see cref="CaseNode"/> representing the complete CASE expression.</returns>
    /// <exception cref="InvalidOperationException">No WHEN branches have been added to the CASE expression</exception>
    public CaseNode Build()
    {
        if (_branches.Count == 0)
        {
            throw new System.InvalidOperationException("A CASE expression requires at least one WHEN ... THEN ... branch.");
        }

        return new CaseNode(
            _branches.ToArray(),
            _elseSql,
            _elseParameters,
            _alias);
    }

    /// <summary>
    /// Converts a <see cref="CaseExpressionBuilder"/> instance to a <see cref="CaseNode"/>.
    /// </summary>
    /// <param name="builder">The builder instance to convert.</param>
    /// <returns>The compiled <see cref="CaseNode"/> AST node.</returns>
    public static implicit operator CaseNode(CaseExpressionBuilder builder) => builder.Build();
}





