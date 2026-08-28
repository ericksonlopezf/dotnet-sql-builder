// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Metadata;

/// <summary>
/// Represents a lightweight identifier for a specific column within an entity's metadata structure.
/// </summary>
/// <param name="Index">The zero-based index of the column in the entity's metadata array.</param>
/// <param name="Name">The name of the column.</param>
public readonly record struct ColumnToken(int Index, string Name);


