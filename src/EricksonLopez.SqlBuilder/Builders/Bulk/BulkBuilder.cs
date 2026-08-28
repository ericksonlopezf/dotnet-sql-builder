// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Builders.Bulk;

using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.ColumnSelection;
using EricksonLopez.SqlBuilder.ColumnSelection.Rules;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

/// <summary>
/// Provides a fluent API for configuring and constructing highly optimized AOT-compatible bulk SQL operations.
/// </summary>
/// <typeparam name="T">The type of the entities, which must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
public sealed class BulkBuilder<T> where T : IStaticEntityMetadata<T>
{
    private readonly IEnumerable<T> _entities;
    private readonly List<IColumnSelectionRule<T>> _rules = new();
    
    private int _batchSize = 5000;

    internal BulkBuilder(IEnumerable<T> entities)
    {
        _entities = entities;
        _rules.Add(new ExcludeGeneratedRule<T>());
    }

    /// <summary>
    /// Configures the maximum number of entities to process in a single bulk batch.
    /// </summary>
    /// <param name="batchSize">The maximum number of entities per batch. Default is 5000.</param>
    /// <returns>The current <see cref="BulkBuilder{T}"/> instance for fluent chaining.</returns>
    public BulkBuilder<T> WithBatchSize(int batchSize)
    {
        _batchSize = batchSize;
        return this;
    }

    /// <summary>
    /// Configures the bulk operation to exclude properties that have null values from the SQL statement.
    /// </summary>
    /// <returns>The current <see cref="BulkBuilder{T}"/> instance for fluent chaining.</returns>
    public BulkBuilder<T> IgnoreNulls() 
    { 
        _rules.Add(new IgnoreNullsRule<T>()); 
        return this; 
    }
    
    /// <summary>
    /// Configures the bulk operation to include only the specified columns.
    /// </summary>
    /// <param name="columns">An array of integer identifiers representing the columns to include.</param>
    /// <returns>The current <see cref="BulkBuilder{T}"/> instance for fluent chaining.</returns>
    public BulkBuilder<T> Only(params int[] columns) 
    { 
        _rules.Add(new OnlyColumnsRule<T>(columns)); 
        return this; 
    }
    
    /// <summary>
    /// Configures the bulk operation to exclude database-generated columns (e.g., identity columns, computed columns).
    /// </summary>
    /// <remarks>
    /// This rule is applied by default upon creation of the <see cref="BulkBuilder{T}"/>.
    /// </remarks>
    /// <returns>The current <see cref="BulkBuilder{T}"/> instance for fluent chaining.</returns>
    public BulkBuilder<T> ExcludeGenerated() 
    { 
        _rules.Add(new ExcludeGeneratedRule<T>()); 
        return this; 
    }

    /// <summary>
    /// Finalizes the configuration and creates a bulk INSERT operation.
    /// </summary>
    /// <returns>A new <see cref="IBulkOperation{T}"/> configured for insertion.</returns>
    public IBulkOperation<T> Insert() => new BulkInsertOperation<T>(_entities, _rules, _batchSize);

    /// <summary>
    /// Finalizes the configuration and creates a bulk UPDATE operation.
    /// </summary>
    /// <returns>A new <see cref="IBulkOperation{T}"/> configured for updating.</returns>
    public IBulkOperation<T> Update() => new BulkUpdateOperation<T>(_entities, _rules, _batchSize);

    /// <summary>
    /// Finalizes the configuration and creates a bulk MERGE operation.
    /// </summary>
    /// <returns>A new <see cref="IBulkOperation{T}"/> configured for merging.</returns>
    public IBulkOperation<T> Merge() => new BulkMergeOperation<T>(_entities, _rules, _batchSize);

    /// <summary>
    /// Finalizes the configuration and creates a bulk UPSERT (insert or update) operation.
    /// </summary>
    /// <returns>A new <see cref="IBulkOperation{T}"/> configured for upserting.</returns>
    public IBulkOperation<T> Upsert() => new BulkUpsertOperation<T>(_entities, _rules, _batchSize);

    /// <summary>
    /// Finalizes the configuration and creates a bulk INSERT IGNORE operation.
    /// </summary>
    /// <returns>A new <see cref="IBulkOperation{T}"/> configured for insert-ignore.</returns>
    public IBulkOperation<T> InsertIgnore() => new BulkInsertIgnoreOperation<T>(_entities, _rules, _batchSize);
}



