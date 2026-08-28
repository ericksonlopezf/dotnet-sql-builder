// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Annotations;

/// <summary>
/// Specifies that an enum or property represents a PostgreSQL enum type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Property)]
public class PostgreSqlEnumAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the enum type in PostgreSQL.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlEnumAttribute"/> class.
    /// </summary>
    /// <param name="typeName">The name of the enum type.</param>
    public PostgreSqlEnumAttribute(string typeName) { TypeName = typeName; }
}
