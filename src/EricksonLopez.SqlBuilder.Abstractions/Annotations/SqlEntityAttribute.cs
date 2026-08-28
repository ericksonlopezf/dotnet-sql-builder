// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Annotations;

/// <summary>
/// Specifies that a class is a SQL entity mapped to a specific table.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SqlEntityAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the database table.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlEntityAttribute"/> class.
    /// </summary>
    /// <param name="tableName">The name of the database table.</param>
    public SqlEntityAttribute(string tableName) { TableName = tableName; }
}
