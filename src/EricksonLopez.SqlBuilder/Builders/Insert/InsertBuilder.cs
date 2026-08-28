// Copyright © Erickson Lopez. MIT License.
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.ColumnSelection;
using EricksonLopez.SqlBuilder.ColumnSelection.Rules;

namespace EricksonLopez.SqlBuilder.Builders.Insert;

/// <summary>
/// Provides a builder for constructing AOT-compatible, single-entity INSERT queries dynamically based on entity metadata.
/// </summary>
/// <typeparam name="T">The type of the entity to insert, which must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
public class InsertBuilder<T> where T : IStaticEntityMetadata<T>
{
    private readonly T _entity;
    private readonly List<IColumnSelectionRule<T>> _rules = new();

    /// <summary>
    /// Gets the entity instance being inserted.
    /// </summary>
    public T Entity => _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="InsertBuilder{T}"/> class using the specified entity.
    /// </summary>
    /// <param name="entity">The entity containing the data to insert.</param>
    public InsertBuilder(T entity)
    {
        _entity = entity;
        _rules.Add(new ExcludeGeneratedRule<T>());
    }

    /// <summary>
    /// Adds a custom column selection rule to the insert operation.
    /// </summary>
    /// <param name="rule">The rule that dictates whether a column is included or excluded.</param>
    public void AddRule(IColumnSelectionRule<T> rule)
    {
        _rules.Add(rule);
    }
    
    /// <summary>
    /// Configures the insert operation to exclude properties that have <see langword="null"/> values.
    /// </summary>
    /// <returns>The current <see cref="InsertBuilder{T}"/> instance for fluent chaining.</returns>
    public InsertBuilder<T> IgnoreNulls()
    {
        _rules.Add(new IgnoreNullsRule<T>());
        return this;
    }

    /// <summary>
    /// Configures the insert operation to include only the specified columns.
    /// </summary>
    /// <param name="columns">An array of integer identifiers representing the columns to include.</param>
    /// <returns>The current <see cref="InsertBuilder{T}"/> instance for fluent chaining.</returns>
    public InsertBuilder<T> Only(params int[] columns)
    {
        _rules.Add(new OnlyColumnsRule<T>(columns));
        return this;
    }

    /// <summary>
    /// Configures the insert operation to explicitly exclude the specified columns.
    /// </summary>
    /// <param name="columns">An array of integer identifiers representing the columns to exclude.</param>
    /// <returns>The current <see cref="InsertBuilder{T}"/> instance for fluent chaining.</returns>
    public InsertBuilder<T> Except(params int[] columns)
    {
        _rules.Add(new ExceptColumnsRule<T>(columns));
        return this;
    }

    /// <summary>
    /// Finalizes the configuration and compiles the insert query using the specified renderer.
    /// </summary>
    /// <param name="dialect">The SQL renderer specific to the target database provider.</param>
    /// <returns>The compiled SQL query string and its parameters.</returns>
    /// <exception cref="InvalidOperationException">No columns are selected for the insert operation</exception>
    public SqlResult Build(ISqlRenderer dialect)
    {
        int colCount = T.ColumnCount;
        
        bool[] maskArray = ArrayPool<bool>.Shared.Rent(colCount);
        try
        {
            Span<bool> mask = maskArray.AsSpan(0, colCount);
            
            var rulesSpan = CollectionsMarshal.AsSpan(_rules);
            ColumnSelectionEngine<T>.SelectColumns(_entity, SqlOperation.Insert, rulesSpan, mask);

            if (!mask.Contains(true))
            {
                throw new InvalidOperationException("No columns selected for insert.");
            }

            return dialect.RenderInsert(_entity, mask);
        }
        // Stryker disable once Block : Justification: Finally block for releasing resources is functionally unobservable
        finally
        {
            // Stryker disable once all : Justification: ArrayPool return is unobservable
            ArrayPool<bool>.Shared.Return(maskArray);
        }
    }
}

