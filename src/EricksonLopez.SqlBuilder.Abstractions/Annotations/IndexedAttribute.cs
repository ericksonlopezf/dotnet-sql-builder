// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Annotations;

/// <summary>
/// Specifies that a property represents a column that is indexed in the database.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IndexedAttribute : Attribute
{
}
