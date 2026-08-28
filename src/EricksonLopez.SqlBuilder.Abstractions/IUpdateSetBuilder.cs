// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines the SET stage of an UPDATE query where column modifications and optional JOIN/WHERE clauses can be configured.
/// </summary>
/// <typeparam name="T">The type of the entity being updated.</typeparam>
public interface IUpdateSetBuilder<T> where T : class, new()
{
    /// <summary>
    /// Sets a specific property to a new value.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being updated.</typeparam>
    /// <param name="property">The expression identifying the property.</param>
    /// <param name="value">The new value to assign.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateSetBuilder<T> Set<TProperty>(Expression<Func<T, TProperty>> property, TProperty value);
    
    /// <summary>
    /// Specifies a custom SQL string for the SET clause.
    /// </summary>
    /// <param name="sql">The formatted SQL string.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateSetBuilder<T> Set(FormattableString sql);
    
    /// <summary>
    /// Adds a FROM clause for the UPDATE statement, useful in PostgreSQL updates with joins.
    /// </summary>
    /// <param name="tableName">The name of the table to select from.</param>
    /// <param name="alias">An optional alias for the table.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateSetBuilder<T> From(string tableName, string? alias = null);
    
    /// <summary>
    /// Adds a JOIN clause to the UPDATE statement.
    /// </summary>
    /// <param name="tableName">The name of the table to join.</param>
    /// <param name="alias">The alias for the joined table.</param>
    /// <param name="on">The explicit join condition.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateSetBuilder<T> Join(string tableName, string alias, string on);
    
    /// <summary>
    /// Adds a strongly-typed JOIN clause using a LINQ expression.
    /// </summary>
    /// <typeparam name="TOther">The type of the entity to join.</typeparam>
    /// <param name="onExpression">The expression defining the join condition.</param>
    /// <returns>The current builder instance.</returns>
    IUpdateSetBuilder<T> Join<TOther>(Expression<Func<T, TOther, bool>> onExpression) where TOther : new();
    
    /// <summary>
    /// Adds a WHERE clause to the UPDATE statement using a LINQ expression.
    /// </summary>
    /// <param name="predicate">The expression defining the condition.</param>
    /// <returns>An <see cref="IUpdateWhereBuilder{T}"/> that represents a compilable query.</returns>
    IUpdateWhereBuilder<T> Where(Expression<Func<T, bool>> predicate);
    
    /// <summary>
    /// Adds a WHERE clause to the UPDATE statement using a formattable string.
    /// </summary>
    /// <param name="condition">The formatted SQL condition.</param>
    /// <returns>An <see cref="IUpdateWhereBuilder{T}"/> that represents a compilable query.</returns>
    IUpdateWhereBuilder<T> Where(FormattableString condition);
    
    /// <summary>
    /// Adds a WHERE EXISTS (subquery) condition to the UPDATE statement.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside EXISTS.</param>
    /// <returns>An <see cref="IUpdateWhereBuilder{T}"/> that represents a compilable query.</returns>
    IUpdateWhereBuilder<T> WhereExists(ISqlQuery subquery);
    
    /// <summary>
    /// Adds a WHERE NOT EXISTS (subquery) condition to the UPDATE statement.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside NOT EXISTS.</param>
    /// <returns>An <see cref="IUpdateWhereBuilder{T}"/> that represents a compilable query.</returns>
    IUpdateWhereBuilder<T> WhereNotExists(ISqlQuery subquery);
    
    /// <summary>
    /// Explicitly allows the update query to execute without a WHERE clause, updating all rows in the table.
    /// </summary>
    /// <returns>An <see cref="IUpdateWhereBuilder{T}"/> that represents a compilable query.</returns>
    IUpdateWhereBuilder<T> WhereAll();

    /// <summary>
    /// Appends an optimistic concurrency check with auto-increment behavior on the concurrency token.
    /// </summary>
    /// <typeparam name="TToken">The type of the concurrency token property.</typeparam>
    /// <param name="tokenSelector">The expression identifying the token property.</param>
    /// <param name="expectedValue">The expected current value of the token.</param>
    /// <returns>An <see cref="IUpdateWhereBuilder{T}"/> that represents a compilable query.</returns>
    IUpdateWhereBuilder<T> WithConcurrencyToken<TToken>(Expression<Func<T, TToken>> tokenSelector, TToken expectedValue);

    /// <summary>
    /// Appends an optimistic concurrency check with an explicit new token value.
    /// </summary>
    /// <typeparam name="TToken">The type of the concurrency token property.</typeparam>
    /// <param name="tokenSelector">The expression identifying the token property.</param>
    /// <param name="expectedValue">The expected current value of the token.</param>
    /// <param name="newValue">The explicit new value to assign to the token property.</param>
    /// <returns>An <see cref="IUpdateWhereBuilder{T}"/> that represents a compilable query.</returns>
    IUpdateWhereBuilder<T> WithConcurrencyToken<TToken>(Expression<Func<T, TToken>> tokenSelector, TToken expectedValue, TToken newValue);
}
