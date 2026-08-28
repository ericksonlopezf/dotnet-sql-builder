// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines the final stage of an UPDATE query construction containing at least one condition or explicit whole-table update directive.
/// </summary>
/// <typeparam name="T">The type of the entity being updated.</typeparam>
public interface IUpdateWhereBuilder<T> : IAstQuery where T : class, new()
{
    /// <summary>
    /// Appends an AND condition to the existing WHERE clause.
    /// </summary>
    /// <param name="predicate">The expression defining the condition.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateWhereBuilder<T> And(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Appends an OR condition to the existing WHERE clause.
    /// </summary>
    /// <param name="predicate">The expression defining the condition.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateWhereBuilder<T> Or(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Specifies columns to return after the update operation completes.
    /// </summary>
    /// <param name="columns">The names of the columns to return.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateWhereBuilder<T> Returning(params string[] columns);
    
    /// <summary>
    /// Specifies properties to return after the update operation completes, using a strongly-typed selector.
    /// </summary>
    /// <typeparam name="TResult">The type of the result projection.</typeparam>
    /// <param name="selector">The expression defining the columns to return.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateWhereBuilder<T> Returning<TResult>(Expression<Func<T, TResult>> selector);

    /// <summary>
    /// Appends an optimistic concurrency check with auto-increment behavior on the concurrency token.
    /// </summary>
    /// <typeparam name="TToken">The type of the concurrency token property.</typeparam>
    /// <param name="tokenSelector">The expression identifying the token property.</param>
    /// <param name="expectedValue">The expected current value of the token.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateWhereBuilder<T> WithConcurrencyToken<TToken>(Expression<Func<T, TToken>> tokenSelector, TToken expectedValue);

    /// <summary>
    /// Appends an optimistic concurrency check with an explicit new token value.
    /// </summary>
    /// <typeparam name="TToken">The type of the concurrency token property.</typeparam>
    /// <param name="tokenSelector">The expression identifying the token property.</param>
    /// <param name="expectedValue">The expected current value of the token.</param>
    /// <param name="newValue">The explicit new value to assign to the token property.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateWhereBuilder<T> WithConcurrencyToken<TToken>(Expression<Func<T, TToken>> tokenSelector, TToken expectedValue, TToken newValue);
}
