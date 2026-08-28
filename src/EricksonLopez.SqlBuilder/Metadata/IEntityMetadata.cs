// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Metadata;

/// <summary>
/// Defines the structure of metadata required to perform zero-allocation SQL operations on an entity type.
/// </summary>
/// <typeparam name="T">The type of the entity this metadata describes.</typeparam>
public interface IEntityMetadata<T>
{
    /// <summary>
    /// Gets the name of the database table associated with the entity.
    /// </summary>
    string TableName { get; }
    
    /// <summary>
    /// Gets a read-only span of metadata for all columns mapped to the entity.
    /// </summary>
    ReadOnlySpan<ColumnMetadata> Columns { get; }

    /// <summary>
    /// Determines whether the value of the specified column for the given entity instance is <see langword="null"/>.
    /// </summary>
    /// <param name="entity">The entity instance to inspect.</param>
    /// <param name="columnIndex">The index of the column within the <see cref="Columns"/> span.</param>
    /// <returns><see langword="true"/> if the property value is null; otherwise, <see langword="false"/>.</returns>
    bool IsNull(T entity, int columnIndex);
    
    /// <summary>
    /// Determines whether the value of the specified column for the given entity instance is its default value.
    /// </summary>
    /// <param name="entity">The entity instance to inspect.</param>
    /// <param name="columnIndex">The index of the column within the <see cref="Columns"/> span.</param>
    /// <returns><see langword="true"/> if the property value equals the default for its type; otherwise, <see langword="false"/>.</returns>
    bool IsDefault(T entity, int columnIndex);
    
    /// <summary>
    /// Retrieves the value of the specified column from the given entity instance, boxed as an object.
    /// </summary>
    /// <param name="entity">The entity instance to extract the value from.</param>
    /// <param name="columnIndex">The index of the column within the <see cref="Columns"/> span.</param>
    /// <returns>The property value as an <see cref="object"/>.</returns>
    object? GetBoxedValue(T entity, int columnIndex);

    /// <summary>
    /// Transposes a batch of entities into individual column arrays and registers them with the parameter manager.
    /// </summary>
    /// <remarks>
    /// This method is critical for highly optimized bulk operations based on parametric arrays (e.g., PostgreSQL UNNEST).
    /// </remarks>
    /// <param name="entities">A read-only span containing the batch of entities to transpose.</param>
    /// <param name="activeColumns">A bitmask span indicating which columns to include in the extraction.</param>
    /// <param name="parameters">The parameter manager where the resulting arrays will be registered.</param>
    void ExtractColumnArrays(
        ReadOnlySpan<T> entities,
        ReadOnlySpan<bool> activeColumns,
        EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters);
}
