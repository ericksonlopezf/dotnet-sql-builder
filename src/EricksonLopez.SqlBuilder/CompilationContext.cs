// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder;

internal sealed class CompilationContext : IDisposable
{

    /// <summary>
    /// Gets the string builder used to accumulate the SQL statement.
    /// </summary>
    public StringBuilder Sql { get; }
    
    /// <summary>
    /// Gets the parameter manager responsible for holding SQL parameters.
    /// </summary>
    public IParameterManager Parameters { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompilationContext"/> class.
    /// </summary>
    /// <param name="parameters">The parameter manager to associate with this context.</param>
    public CompilationContext(IParameterManager parameters)
    {
        Sql = StringBuilderPool.Get();
        Parameters = parameters;
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        StringBuilderPool.Return(Sql);
    }
}

