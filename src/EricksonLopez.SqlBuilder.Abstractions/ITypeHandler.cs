// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines a handler for converting custom types to and from database parameters.
/// </summary>
public interface ITypeHandler
{
    /// <summary>
    /// Sets the value of a parameter using the custom type.
    /// </summary>
    /// <param name="parameter">The database parameter.</param>
    /// <param name="value">The value to set.</param>
    void SetValue(System.Data.IDbDataParameter parameter, object? value);

    /// <summary>
    /// Parses a database value back to the custom type.
    /// </summary>
    /// <param name="destinationType">The target type to parse to.</param>
    /// <param name="value">The value from the database.</param>
    /// <returns>The parsed object.</returns>
    object? Parse(System.Type destinationType, object? value);
}


