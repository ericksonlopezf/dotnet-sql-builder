// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.Pagination.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Represents a select query bound to an open database connection.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public readonly struct BoundSelectQuery<T> : IAstQuery where T : class, new()
{
    /// <summary>Gets the underlying select query.</summary>
    public SelectQuery<T> Query { get; }
    /// <summary>Gets the database connection bound to this query.</summary>
    public IDbConnection Connection { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="BoundSelectQuery{T}"/>.
    /// </summary>
    /// <param name="query">The underlying select query.</param>
    /// <param name="connection">The bound database connection.</param>
    public BoundSelectQuery(SelectQuery<T> query, IDbConnection connection)
    {
        Query = query ?? throw new ArgumentNullException(nameof(query));
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    IReadOnlyList<ISqlNode> IAstQuery.Nodes => ((IAstQuery)Query).Nodes;

    /// <inheritdoc />
    public string? Tag => Query.Tag;

    /// <inheritdoc cref="ISqlQuery.Build" />
    public SqlResult Build(ISqlCompiler compiler) => Query.Build(compiler);

    /// <inheritdoc cref="SelectQuery{T}.Where(System.Linq.Expressions.Expression{Func{T, bool}})" />
    public BoundSelectQuery<T> Where(System.Linq.Expressions.Expression<Func<T, bool>> predicate) => new(Query.Where(predicate), Connection);

    /// <summary>
    /// Adds a raw WHERE condition to the query.
    /// </summary>
    /// <param name="condition">The raw SQL condition.</param>
    /// <returns>A new <see cref="BoundSelectQuery{T}"/> with the condition applied.</returns>
    public BoundSelectQuery<T> Where(FormattableString condition) => new(Query.Where(condition), Connection);

    /// <summary>
    /// Appends an AND predicate to the WHERE clause.
    /// </summary>
    /// <param name="predicate">The filter condition.</param>
    /// <returns>A new <see cref="BoundSelectQuery{T}"/> with the predicate applied.</returns>
    public BoundSelectQuery<T> And(System.Linq.Expressions.Expression<Func<T, bool>> predicate) => new(Query.And(predicate), Connection);

    /// <summary>
    /// Appends an OR predicate to the WHERE clause.
    /// </summary>
    /// <param name="predicate">The filter condition.</param>
    /// <returns>A new <see cref="BoundSelectQuery{T}"/> with the predicate applied.</returns>
    public BoundSelectQuery<T> Or(System.Linq.Expressions.Expression<Func<T, bool>> predicate) => new(Query.Or(predicate), Connection);

    /// <summary>
    /// Adds an ORDER BY clause in ascending order.
    /// </summary>
    /// <param name="keySelector">The key selector expression.</param>
    /// <returns>A new <see cref="BoundSelectQuery{T}"/> with the ordering applied.</returns>
    public BoundSelectQuery<T> OrderBy(System.Linq.Expressions.Expression<Func<T, object>> keySelector) => new(Query.OrderBy(keySelector), Connection);

    /// <summary>
    /// Adds an ORDER BY clause in descending order.
    /// </summary>
    /// <param name="keySelector">The key selector expression.</param>
    /// <returns>A new <see cref="BoundSelectQuery{T}"/> with the ordering applied.</returns>
    public BoundSelectQuery<T> OrderByDescending(System.Linq.Expressions.Expression<Func<T, object>> keySelector) => new(Query.OrderByDescending(keySelector), Connection);

    /// <summary>
    /// Applies LIMIT/OFFSET pagination to the query.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <returns>A new <see cref="BoundSelectQuery{T}"/> with pagination applied.</returns>
    public BoundSelectQuery<T> Paginate(int pageNumber, int pageSize) => new(Query.Paginate(pageNumber, pageSize), Connection);

    /// <summary>
    /// Projects this query to a different result type.
    /// </summary>
    /// <typeparam name="TResult">The target projection type.</typeparam>
    /// <returns>A new <see cref="BoundSelectQuery{TResult}"/> instance.</returns>
    public BoundSelectQuery<TResult> ProjectTo<TResult>() where TResult : class, new() => new(Query.ProjectTo<T, TResult>(), Connection);

    /// <summary>
    /// Executes the query and returns a <see cref="Result{T}"/> containing a read-only list of items, or an error.
    /// </summary>
    /// <param name="transaction">An optional database transaction.</param>
    /// <returns>A <see cref="Result{T}"/> containing the query results.</returns>
    public Task<Result<IReadOnlyList<T>>> ToResultAsync(IDbTransaction? transaction = null) => Query.ToResultAsync(Connection, transaction);

    /// <summary>
    /// Executes the query with pagination and returns a <see cref="Result{T}"/> containing the paged list, or an error.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <returns>A <see cref="Result{T}"/> containing the paginated list.</returns>
    public Task<Result<IPagedList<T>>> ToPagedListAsync(int pageNumber, int pageSize, IDbTransaction? transaction = null) => Query.ToPagedListAsync(Connection, pageNumber, pageSize, transaction);

    /// <summary>
    /// Executes the query and returns an unbuffered asynchronous stream of items.
    /// </summary>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An asynchronous stream of items.</returns>
    public IAsyncEnumerable<T> ToStreamAsync(IDbTransaction? transaction = null, CancellationToken cancellationToken = default) => Query.ToStreamAsync(Connection, transaction, cancellationToken);
}
