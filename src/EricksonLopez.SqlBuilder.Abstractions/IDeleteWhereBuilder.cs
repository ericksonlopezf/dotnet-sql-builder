// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines the final stage of a DELETE query construction containing at least one condition or explicit whole-table delete directive.
/// </summary>
/// <typeparam name="T">The type of the entity being deleted.</typeparam>
public interface IDeleteWhereBuilder<T> : IAstQuery where T : class, new()
{
    /// <summary>
    /// Appends an AND condition to the existing WHERE clause.
    /// </summary>
    /// <param name="predicate">The expression defining the condition.</param>
    /// <returns>The current builder instance.</returns>
    IDeleteWhereBuilder<T> And(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Appends an OR condition to the existing WHERE clause.
    /// </summary>
    /// <param name="predicate">The expression defining the condition.</param>
    /// <returns>The current builder instance.</returns>
    IDeleteWhereBuilder<T> Or(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Specifies columns to return after the delete operation completes.
    /// </summary>
    /// <param name="columns">The names of the columns to return.</param>
    /// <returns>The current builder instance.</returns>
    IDeleteWhereBuilder<T> Returning(params string[] columns);

    /// <summary>
    /// Specifies properties to return after the delete operation completes, using a strongly-typed selector.
    /// </summary>
    /// <typeparam name="TResult">The type of the result projection.</typeparam>
    /// <param name="selector">The expression defining the columns to return.</param>
    /// <returns>The current builder instance.</returns>
    IDeleteWhereBuilder<T> Returning<TResult>(Expression<Func<T, TResult>> selector);
}
