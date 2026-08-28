// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.Oracle;

/// <summary>
/// Custom parameter manager for Oracle that uses ':' prefix and translates booleans.
/// </summary>
public class OracleParameterManager : IParameterManager
{
    private readonly ParameterManager _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleParameterManager"/> class.
    /// </summary>
    /// <param name="prefix">The parameter name prefix.</param>
    public OracleParameterManager(string prefix = ":")
    {
        _inner = new ParameterManager(prefix);
    }

    /// <inheritdoc />
    public string Add(object? value) => _inner.Add(Process(value));
    /// <summary>Adds a strongly-typed parameter.</summary>
    public string Add<TParam>(TParam value) => _inner.Add(Process(value));
    /// <inheritdoc />
    public string AddNamed(string name, object? value) => _inner.AddNamed(name, Process(value));
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> GetParameters() => _inner.GetParameters();

    private object? Process(object? value)
    {
        if (value is bool b)
        {
            return b ? 1 : 0;
        }

        return value;
    }
}
