// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines a parameter collection and naming manager used during SQL statement compilation.
/// </summary>
public interface IParameterManager
{
    /// <summary>
    /// Adds a parameter value and returns its generated parameter name in the dialect's syntax.
    /// </summary>
    /// <param name="value">The value to add as a parameter.</param>
    /// <returns>The generated parameter name (e.g., '@p0').</returns>
    string Add(object? value);
    
    /// <summary>
    /// Adds a named parameter value and returns its formatted parameter name in the dialect's syntax.
    /// </summary>
    /// <param name="name">The explicit name of the parameter.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>The formatted parameter name (e.g., '@name').</returns>
    string AddNamed(string name, object? value);
    
    /// <summary>
    /// Gets the read-only dictionary of all registered parameters.
    /// </summary>
    /// <returns>A dictionary containing the parameter names and their values.</returns>
    IReadOnlyDictionary<string, object?> GetParameters();
}

