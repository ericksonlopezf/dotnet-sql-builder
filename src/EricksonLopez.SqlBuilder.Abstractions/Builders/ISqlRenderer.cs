// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.Builders;

/// <summary>
/// Defines a contract for rendering SQL operations based on entity metadata and selected column masks.
/// Dialect-specific renderers implement this interface to provide optimized, AOT-compatible SQL generation.
/// </summary>
public interface ISqlRenderer
{
    /// <summary>
    /// Renders an INSERT statement for a single entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity instance.</param>
    /// <param name="insertMask">A span representing which columns to include in the INSERT.</param>
    /// <returns>A <see cref="SqlResult"/> containing the generated SQL and parameters.</returns>
    SqlResult RenderInsert<T>(T entity, Span<bool> insertMask) where T : IStaticEntityMetadata<T>;
    
    /// <summary>
    /// Renders an UPDATE statement for a single entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity instance.</param>
    /// <param name="setMask">A span representing which columns to include in the SET clause.</param>
    /// <param name="whereMask">A span representing which columns to use in the WHERE clause (usually primary keys).</param>
    /// <returns>A <see cref="SqlResult"/> containing the generated SQL and parameters.</returns>
    SqlResult RenderUpdate<T>(T entity, Span<bool> setMask, Span<bool> whereMask) where T : IStaticEntityMetadata<T>;

    /// <summary>
    /// Renders a batch of bulk INSERT statements for a collection of entities.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <param name="rules">The column selection rules applied to the operation.</param>
    /// <param name="batchSize">The maximum number of entities to process per batch.</param>
    /// <returns>A <see cref="EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult"/> containing the generated batches.</returns>
    EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkInsert<T>(
        IEnumerable<T> entities, 
        List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, 
        int batchSize) where T : IStaticEntityMetadata<T>;

    /// <summary>
    /// Renders a batch of bulk UPDATE statements for a collection of entities.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <param name="entities">The collection of entities to update.</param>
    /// <param name="rules">The column selection rules applied to the operation.</param>
    /// <param name="batchSize">The maximum number of entities to process per batch.</param>
    /// <returns>A <see cref="EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult"/> containing the generated batches.</returns>
    EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkUpdate<T>(
        IEnumerable<T> entities, 
        List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, 
        int batchSize) where T : IStaticEntityMetadata<T>;

    /// <summary>
    /// Renders a batch of bulk MERGE statements for a collection of entities.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <param name="entities">The collection of entities to merge.</param>
    /// <param name="rules">The column selection rules applied to the operation.</param>
    /// <param name="batchSize">The maximum number of entities to process per batch.</param>
    /// <returns>A <see cref="EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult"/> containing the generated batches.</returns>
    EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkMerge<T>(
        IEnumerable<T> entities, 
        List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, 
        int batchSize) where T : IStaticEntityMetadata<T>;

    /// <summary>
    /// Renders a batch of bulk UPSERT statements for a collection of entities.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <param name="entities">The collection of entities to upsert.</param>
    /// <param name="rules">The column selection rules applied to the operation.</param>
    /// <param name="batchSize">The maximum number of entities to process per batch.</param>
    /// <returns>A <see cref="EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult"/> containing the generated batches.</returns>
    EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkUpsert<T>(
        IEnumerable<T> entities, 
        List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, 
        int batchSize) where T : IStaticEntityMetadata<T>;

    /// <summary>
    /// Renders a batch of bulk INSERT IGNORE statements for a collection of entities.
    /// </summary>
    /// <typeparam name="T">The type of the entities.</typeparam>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <param name="rules">The column selection rules applied to the operation.</param>
    /// <param name="batchSize">The maximum number of entities to process per batch.</param>
    /// <returns>A <see cref="EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult"/> containing the generated batches.</returns>
    EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult RenderBulkInsertIgnore<T>(
        IEnumerable<T> entities, 
        List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<T>> rules, 
        int batchSize) where T : IStaticEntityMetadata<T>;
}


