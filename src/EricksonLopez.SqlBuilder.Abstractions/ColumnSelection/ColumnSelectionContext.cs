// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.ColumnSelection;

/// <summary>
/// Provides contextual information during the execution of column selection rules.
/// </summary>
/// <typeparam name="TEntity">The type of the entity whose columns are being processed.</typeparam>
public ref struct ColumnSelectionContext<TEntity> where TEntity : IStaticEntityMetadata<TEntity>
{
    /// <summary>
    /// Gets the entity instance being evaluated.
    /// </summary>
    public TEntity Entity { get; }
    
    /// <summary>
    /// Gets an optional snapshot entity representing the original state, used for diff-based updates.
    /// </summary>
    public TEntity? Snapshot { get; }
    
    /// <summary>
    /// Gets the type of SQL operation being built (e.g., Insert, Update).
    /// </summary>
    public SqlOperation Operation { get; }
    
    /// <summary>
    /// Gets a mutable span representing a bitmask of columns. True indicates inclusion, False indicates exclusion.
    /// </summary>
    public Span<bool> IncludedColumns { get; } 
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnSelectionContext{TEntity}"/> struct.
    /// </summary>
    /// <param name="entity">The primary entity instance.</param>
    /// <param name="operation">The SQL operation context.</param>
    /// <param name="includedColumns">The bitmask tracking column inclusion.</param>
    /// <param name="snapshot">An optional snapshot of the entity for diff comparisons.</param>
    public ColumnSelectionContext(TEntity entity, SqlOperation operation, Span<bool> includedColumns, TEntity? snapshot = default)
    {
        Entity = entity;
        Operation = operation;
        IncludedColumns = includedColumns;
        Snapshot = snapshot;
    }

    /// <summary>
    /// Explicitly excludes a specific column from the final SQL statement.
    /// </summary>
    /// <param name="token">The token representing the column to exclude.</param>
    public void Exclude(ColumnToken token) => IncludedColumns[token.Index] = false;
    
    /// <summary>
    /// Explicitly includes a specific column in the final SQL statement.
    /// </summary>
    /// <param name="token">The token representing the column to include.</param>
    public void Include(ColumnToken token) => IncludedColumns[token.Index] = true;
    
    /// <summary>
    /// Determines whether the value of the specified column index is null.
    /// </summary>
    /// <param name="index">The zero-based index of the column.</param>
    /// <returns><see langword="true"/> if the column value is <see langword="null"/>; otherwise, <see langword="false"/>.</returns>
    public bool IsNull(int index) => TEntity.IsNull(Entity, index);
    
    /// <summary>
    /// Determines whether the value of the specified column index is its default value.
    /// </summary>
    /// <param name="index">The zero-based index of the column.</param>
    /// <returns><see langword="true"/> if the column value is its default value; otherwise, <see langword="false"/>.</returns>
    public bool IsDefault(int index) => TEntity.IsDefault(Entity, index);
    
    /// <summary>
    /// Determines whether the value of the specified column index is equal to the value in the snapshot.
    /// </summary>
    /// <param name="index">The zero-based index of the column.</param>
    /// <returns><see langword="true"/> if the current value matches the snapshot; otherwise, <see langword="false"/>.</returns>
    public bool AreEqual(int index) => Snapshot is not null && TEntity.AreEqual(Entity, Snapshot, index);
}
