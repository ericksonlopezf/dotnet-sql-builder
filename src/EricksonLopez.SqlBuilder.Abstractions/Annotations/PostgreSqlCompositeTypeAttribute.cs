// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Annotations;

/// <summary>
/// Specifies that a class represents a PostgreSQL composite type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PostgreSqlCompositeTypeAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the composite type.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlCompositeTypeAttribute"/> class.
    /// </summary>
    /// <param name="typeName">The name of the composite type.</param>
    public PostgreSqlCompositeTypeAttribute(string typeName) { TypeName = typeName; }
}
