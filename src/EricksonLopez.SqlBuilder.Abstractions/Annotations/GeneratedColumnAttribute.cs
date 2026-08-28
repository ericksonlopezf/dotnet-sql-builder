// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Annotations;

/// <summary>
/// Specifies that a property represents a computed or generated column in the database.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class GeneratedColumnAttribute : Attribute
{
}
