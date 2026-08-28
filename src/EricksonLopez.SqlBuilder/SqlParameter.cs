// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Represents a parameterized value used in SQL queries with optional type metadata.
/// </summary>
/// <remarks>
/// This struct allows passing explicitly typed parameters (such as JSON or array types) 
/// to the underlying database driver (like Dapper or ADO.NET) without requiring reflection.
/// </remarks>
public readonly struct SqlParameter
{
    /// <summary>
    /// Gets the raw value of the parameter.
    /// </summary>
    public object? Value { get; }
    
    /// <summary>
    /// Gets the optional database type to explicitly map the parameter.
    /// </summary>
    public DbType? DbType { get; }
    
    /// <summary>
    /// Gets the optional provider-specific database type name (e.g., "jsonb").
    /// </summary>
    public string? DatabaseTypeName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlParameter"/> struct with the specified value and optional type metadata.
    /// </summary>
    /// <param name="value">The raw value of the parameter.</param>
    /// <param name="dbType">The standard database type of the parameter.</param>
    /// <param name="dbTypeName">The provider-specific database type name.</param>
    public SqlParameter(object? value, DbType? dbType = null, string? dbTypeName = null)
    {
        Value = value;
        DbType = dbType;
        DatabaseTypeName = dbTypeName;
    }
}


