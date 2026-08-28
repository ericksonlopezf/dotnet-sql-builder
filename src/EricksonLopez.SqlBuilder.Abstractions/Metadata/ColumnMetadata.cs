// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Metadata;

/// <summary>
/// Provides metadata for a database column associated with an entity property.
/// </summary>
/// <param name="Index">The zero-based index of the column within the entity's column list.</param>
/// <param name="Name">The mapped name of the column in the database.</param>
/// <param name="Flags">The characteristics of the column.</param>
public readonly record struct ColumnMetadata(int Index, string Name, ColumnFlags Flags)
{
    /// <summary>
    /// Determines whether the column has the specified flag.
    /// </summary>
    /// <param name="flag">The flag to check.</param>
    /// <returns><see langword="true"/> if the column has the specified flag; otherwise, <see langword="false"/>.</returns>
    public bool HasFlag(ColumnFlags flag) => (Flags & flag) == flag;
}

