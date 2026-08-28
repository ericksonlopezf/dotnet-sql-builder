// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EricksonLopez.SqlBuilder;

internal sealed class ParameterManager : EricksonLopez.SqlBuilder.Abstractions.IParameterManager
{
    private readonly Dictionary<string, object?> _parameters = new();
    private int _counter = 0;
    private readonly string _prefix;
    private readonly int _maxParameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParameterManager"/> class.
    /// </summary>
    /// <param name="prefix">The prefix used for parameter names (e.g., '@', ':', or '$').</param>
    /// <param name="maxParameters">The maximum number of parameters allowed before throwing an exception.</param>
    public ParameterManager(string prefix = "@", int maxParameters = int.MaxValue)
    {
        _prefix = prefix;
        _maxParameters = maxParameters;
    }

    /// <summary>
    /// Adds a parameter with the specified value and auto-generates its name.
    /// </summary>
    /// <param name="value">The value to bind to the parameter.</param>
    /// <returns>The generated parameter name including the prefix.</returns>
    /// <exception cref="InvalidOperationException">The maximum number of parameters is exceeded</exception>
    public string Add(object? value)
    {
        if (_counter >= _maxParameters)
        {
            throw new System.InvalidOperationException($"Maximum number of parameters ({_maxParameters}) exceeded.");
        }
        var name = $"p{_counter++}";
        _parameters[name] = ProcessValue(value);
        return string.Concat(_prefix, name);
    }

    /// <summary>
    /// Adds a named parameter with the specified value.
    /// </summary>
    /// <param name="name">The name of the parameter without the prefix.</param>
    /// <param name="value">The value to bind to the parameter.</param>
    /// <returns>The parameter name including the prefix.</returns>
    public string AddNamed(string name, object? value)
    {
        _parameters[name] = ProcessValue(value);
        return string.Concat(_prefix, name);
    }
    
    private object? ProcessValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        var type = value.GetType();
        
        if (Sql.TypeHandlers.TryGetValue(type, out var handler))
        {
            return handler.Parse(type, value);
        }
        
        return value;
    }

    /// <summary>
    /// Gets the read-only dictionary containing all bound parameters.
    /// </summary>
    /// <returns>A dictionary of parameter names and their associated values.</returns>
    public IReadOnlyDictionary<string, object?> GetParameters() => _parameters;
}


