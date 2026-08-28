// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines the initial stage of a DELETE query construction where table sources, joins, and using clauses can be specified.
/// </summary>
/// <typeparam name="T">The type of the target entity to delete from.</typeparam>
public interface IDeleteFromBuilder<T> where T : class, new()
{
    /// <summary>
    /// Adds a WHERE clause to the DELETE statement using a strongly-typed LINQ predicate.
    /// </summary>
    /// <param name="predicate">The expression defining the condition.</param>
    /// <returns>An <see cref="IDeleteWhereBuilder{T}"/> that represents a compilable query.</returns>
    IDeleteWhereBuilder<T> Where(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Adds a WHERE clause to the DELETE statement using a parameterized formattable string.
    /// </summary>
    /// <param name="condition">The formatted SQL condition.</param>
    /// <returns>An <see cref="IDeleteWhereBuilder{T}"/> that represents a compilable query.</returns>
    IDeleteWhereBuilder<T> Where(FormattableString condition);

    /// <summary>
    /// Adds a WHERE EXISTS (subquery) condition to the DELETE statement.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside EXISTS.</param>
    /// <returns>An <see cref="IDeleteWhereBuilder{T}"/> that represents a compilable query.</returns>
    IDeleteWhereBuilder<T> WhereExists(ISqlQuery subquery);

    /// <summary>
    /// Adds a WHERE NOT EXISTS (subquery) condition to the DELETE statement.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside NOT EXISTS.</param>
    /// <returns>An <see cref="IDeleteWhereBuilder{T}"/> that represents a compilable query.</returns>
    IDeleteWhereBuilder<T> WhereNotExists(ISqlQuery subquery);

    /// <summary>
    /// Explicitly allows the delete query to execute without a WHERE clause, deleting all rows in the table.
    /// </summary>
    /// <returns>An <see cref="IDeleteWhereBuilder{T}"/> that represents a compilable query.</returns>
    IDeleteWhereBuilder<T> WhereAll();
}
