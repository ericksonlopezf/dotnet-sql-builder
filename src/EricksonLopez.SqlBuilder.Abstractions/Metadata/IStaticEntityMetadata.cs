// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;

namespace EricksonLopez.SqlBuilder.Abstractions.Metadata;

/// <summary>
/// Defines the static metadata and operations required for AOT-optimized query generation
/// for a specific entity type.
/// </summary>
/// <typeparam name="TEntity">The type of the entity associated with this metadata.</typeparam>
public interface IStaticEntityMetadata<TEntity>
{
    /// <summary>
    /// Gets the name of the database table associated with the entity.
    /// </summary>
    static abstract string TableName { get; }

    /// <summary>
    /// Gets the total number of columns mapped for the entity.
    /// </summary>
    static abstract int ColumnCount { get; }

    /// <summary>
    /// Retrieves a read-only span containing the metadata for all mapped columns.
    /// </summary>
    /// <returns>A read-only span of <see cref="ColumnMetadata"/>.</returns>
    static abstract ReadOnlySpan<ColumnMetadata> GetColumns();
    
    /// <summary>
    /// Determines whether the value of the specified column is null for the given entity.
    /// </summary>
    /// <param name="entity">The entity instance to evaluate.</param>
    /// <param name="columnIndex">The zero-based index of the column.</param>
    /// <returns><see langword="true"/> if the column value is null; otherwise, <see langword="false"/>.</returns>
    static abstract bool IsNull(TEntity entity, int columnIndex);

    /// <summary>
    /// Determines whether the value of the specified column is the default value for the given entity.
    /// </summary>
    /// <param name="entity">The entity instance to evaluate.</param>
    /// <param name="columnIndex">The zero-based index of the column.</param>
    /// <returns><see langword="true"/> if the column value is the default value; otherwise, <see langword="false"/>.</returns>
    static abstract bool IsDefault(TEntity entity, int columnIndex);

    /// <summary>
    /// Determines whether the value of the specified column is equal between the current entity and a snapshot.
    /// </summary>
    /// <param name="entity">The current entity instance.</param>
    /// <param name="snapshot">The original snapshot of the entity.</param>
    /// <param name="columnIndex">The zero-based index of the column to compare.</param>
    /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
    static abstract bool AreEqual(TEntity entity, TEntity snapshot, int columnIndex);
    
    /// <summary>
    /// Retrieves the database column name for the specified column index.
    /// </summary>
    /// <param name="columnIndex">The zero-based index of the column.</param>
    /// <returns>The physical name of the column in the database.</returns>
    static abstract string GetColumnName(int columnIndex);

    /// <summary>
    /// Binds the value of the specified column to the provided parameter manager and returns the parameter name.
    /// </summary>
    /// <param name="entity">The entity containing the value to bind.</param>
    /// <param name="columnIndex">The zero-based index of the column.</param>
    /// <param name="parameters">The parameter manager responsible for holding the bound parameter.</param>
    /// <returns>The name of the bound parameter.</returns>
    static abstract string BindParameter(TEntity entity, int columnIndex, IParameterManager parameters);

    /// <summary>
    /// Extracts a batch of entities into column-based arrays and binds them to the parameter manager.
    /// </summary>
    /// <remarks>
    /// This method is critical for bulk operations that rely on parameter arrays (e.g., PostgreSQL COPY or array unnesting).
    /// </remarks>
    /// <param name="entities">A read-only span of entities to extract.</param>
    /// <param name="activeColumns">A read-only span of booleans indicating which columns should be extracted.</param>
    /// <param name="parameters">The parameter manager where the extracted arrays will be bound.</param>
    static abstract void ExtractColumnArrays(
        ReadOnlySpan<TEntity> entities,
        ReadOnlySpan<bool> activeColumns,
        IParameterManager parameters);

    /// <summary>
    /// Reads and parses a single entity instance from an active <see cref="System.Data.IDataReader"/> row.
    /// </summary>
    /// <param name="reader">The active data reader positioned at a valid row.</param>
    /// <returns>A populated instance of <typeparamref name="TEntity"/>.</returns>
    static abstract TEntity FromReader(System.Data.IDataReader reader);

    /// <summary>
    /// Returns a high-performance, reusable reader parser delegate for <typeparamref name="TEntity"/>.
    /// </summary>
    /// <returns>A delegate that reads and maps reader rows into <typeparamref name="TEntity"/>.</returns>
    static abstract Func<System.Data.IDataReader, TEntity> GetReaderParser();
}

