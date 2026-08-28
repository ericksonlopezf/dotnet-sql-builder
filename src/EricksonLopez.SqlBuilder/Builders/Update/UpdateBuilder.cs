// Copyright © Erickson Lopez. MIT License.
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.ColumnSelection;
using EricksonLopez.SqlBuilder.ColumnSelection.Rules;

namespace EricksonLopez.SqlBuilder.Builders.Update;

/// <summary>
/// Provides a builder for constructing AOT-compatible, single-entity UPDATE queries dynamically based on entity metadata.
/// </summary>
/// <remarks>
/// By default, primary keys are excluded from the SET clause and automatically mapped to the WHERE clause.
/// </remarks>
/// <typeparam name="T">The type of the entity to update, which must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
public class UpdateBuilder<T> where T : IStaticEntityMetadata<T>
{
    private readonly T _entity;
    private readonly List<IColumnSelectionRule<T>> _rules = new();

    /// <summary>
    /// Gets the entity instance containing the updated state.
    /// </summary>
    public T Entity => _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateBuilder{T}"/> class using the specified entity.
    /// </summary>
    /// <param name="entity">The entity containing the modifications.</param>
    public UpdateBuilder(T entity)
    {
        _entity = entity;
        _rules.Add(new ExcludePrimaryKeysRule<T>());
        _rules.Add(new ExcludeGeneratedRule<T>());
    }

    /// <summary>
    /// Adds a custom column selection rule to the update operation.
    /// </summary>
    /// <param name="rule">The rule that dictates whether a column is included or excluded.</param>
    public void AddRule(IColumnSelectionRule<T> rule)
    {
        _rules.Add(rule);
    }
    
    /// <summary>
    /// Configures the update operation to exclude properties that have <see langword="null"/> values.
    /// </summary>
    /// <returns>The current <see cref="UpdateBuilder{T}"/> instance for fluent chaining.</returns>
    public UpdateBuilder<T> IgnoreNulls()
    {
        _rules.Add(new IgnoreNullsRule<T>());
        return this;
    }

    /// <summary>
    /// Configures the update operation to include only the specified columns in the SET clause.
    /// </summary>
    /// <param name="columns">An array of integer identifiers representing the columns to update.</param>
    /// <returns>The current <see cref="UpdateBuilder{T}"/> instance for fluent chaining.</returns>
    public UpdateBuilder<T> Only(params int[] columns)
    {
        _rules.Add(new OnlyColumnsRule<T>(columns));
        return this;
    }

    /// <summary>
    /// Configures the update operation to explicitly exclude the specified columns from the SET clause.
    /// </summary>
    /// <param name="columns">An array of integer identifiers representing the columns to exclude.</param>
    /// <returns>The current <see cref="UpdateBuilder{T}"/> instance for fluent chaining.</returns>
    public UpdateBuilder<T> Except(params int[] columns)
    {
        _rules.Add(new ExceptColumnsRule<T>(columns));
        return this;
    }

    /// <summary>
    /// Finalizes the configuration and compiles the update query using the specified renderer.
    /// </summary>
    /// <param name="dialect">The SQL renderer specific to the target database provider.</param>
    /// <returns>The compiled SQL query string and its parameters.</returns>
    /// <exception cref="InvalidOperationException">No columns are selected for the SET clause, or no primary keys could be identified for the WHERE clause</exception>
    public SqlResult Build(ISqlRenderer dialect)
    {
        int colCount = T.ColumnCount;
        
        bool[] setMaskArray = ArrayPool<bool>.Shared.Rent(colCount);
        bool[] whereMaskArray = ArrayPool<bool>.Shared.Rent(colCount);
        try
        {
            Span<bool> setMask = setMaskArray.AsSpan(0, colCount);
            Span<bool> whereMask = whereMaskArray.AsSpan(0, colCount);
            // Stryker disable once Statement : ArrayPool rented buffer zero-initialization safety
            whereMask.Clear();
            
            for (int i = 0; i < colCount; i++)
            {
                var flags = T.GetColumns()[i].Flags;
                if ((flags & ColumnFlags.PrimaryKey) != 0)
                {
                    whereMask[i] = true;
                }
            }

            var rulesSpan = CollectionsMarshal.AsSpan(_rules);
            ColumnSelectionEngine<T>.SelectColumns(_entity, SqlOperation.Update, rulesSpan, setMask);

            if (!setMask.Contains(true))
            {
                throw new InvalidOperationException("No columns selected for SET clause.");
            }
            
            if (!whereMask.Contains(true))
            {
                throw new InvalidOperationException("No columns selected for WHERE clause. Unconditional updates must use the AST query builder.");
            }

            return dialect.RenderUpdate(_entity, setMask, whereMask);
        }
        // Stryker disable once Block : Justification: Finally block for releasing resources is functionally unobservable
        finally
        {
            // Stryker disable once all : Justification: ArrayPool return is unobservable
            ArrayPool<bool>.Shared.Return(setMaskArray);
            // Stryker disable once all : Justification: ArrayPool return is unobservable
            ArrayPool<bool>.Shared.Return(whereMaskArray);
        }
    }
}

