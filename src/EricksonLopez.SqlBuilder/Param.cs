// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Provides factory methods for creating explicitly typed SQL parameters.
/// </summary>
public static class Param
{
    /// <summary>
    /// Creates a SQL parameter explicitly marked as the 'json' database type.
    /// </summary>
    /// <param name="value">The value to serialize or pass as JSON.</param>
    /// <returns>A new <see cref="SqlParameter"/> configured for JSON.</returns>
    public static SqlParameter Json(object value) => new SqlParameter(value, dbTypeName: "json");
    
    /// <summary>
    /// Creates a SQL parameter explicitly marked as the 'jsonb' database type (PostgreSQL specific).
    /// </summary>
    /// <param name="value">The value to serialize or pass as JSONB.</param>
    /// <returns>A new <see cref="SqlParameter"/> configured for JSONB.</returns>
    public static SqlParameter Jsonb(object value) => new SqlParameter(value, dbTypeName: "jsonb");
    
    /// <summary>
    /// Creates a SQL parameter containing an array of values.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the array.</typeparam>
    /// <param name="values">The array of values.</param>
    /// <returns>A new <see cref="SqlParameter"/> containing the array.</returns>
    public static SqlParameter Array<T>(T[] values) => new SqlParameter(values);
    
    /// <summary>
    /// Creates a SQL parameter optimized for IN clause expansion.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the collection.</typeparam>
    /// <param name="values">The collection of values to match against.</param>
    /// <returns>A new <see cref="SqlParameter"/> configured for an IN clause.</returns>
    public static SqlParameter In<T>(IEnumerable<T> values) => new SqlParameter(values);
    
    /// <summary>
    /// Creates a SQL parameter explicitly marked with a custom composite type name.
    /// </summary>
    /// <param name="value">The composite value.</param>
    /// <param name="compositeTypeName">The name of the composite type in the database.</param>
    /// <returns>A new <see cref="SqlParameter"/> configured with the composite type name.</returns>
    public static SqlParameter Composite(object value, string compositeTypeName) => new SqlParameter(value, dbTypeName: compositeTypeName);
    
    /// <summary>
    /// Creates a SQL parameter that converts the specified enumeration value to its string representation.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enumeration.</typeparam>
    /// <param name="value">The enumeration value to convert.</param>
    /// <returns>A new <see cref="SqlParameter"/> containing the string representation of the enumeration.</returns>
    public static SqlParameter EnumAsString<TEnum>(TEnum value) where TEnum : Enum => new SqlParameter(value.ToString());
}
