// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;
using static EricksonLopez.Result.Result;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Provides extension methods on <see cref="IDbConnection"/> for starting a fluent SQL builder chain.
/// </summary>
public static class ConnectionSqlExtensions
{
    /// <summary>
    /// Initiates a SQL builder context on the current connection.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <returns>A <see cref="SqlBuilderConnectionContext"/> bound to the connection.</returns>
    public static SqlBuilderConnectionContext Sql(this IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return new SqlBuilderConnectionContext(connection);
    }

    /// <summary>
    /// Applies pagination limits to the specified select query.
    /// </summary>
    /// <typeparam name="T">The type of the query result.</typeparam>
    /// <param name="query">The query to paginate.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of records per page.</param>
    /// <returns>The paginated query.</returns>
    public static SelectQuery<T> Paginate<T>(this SelectQuery<T> query, int pageNumber, int pageSize) where T : class, new()
    {
        return query.Limit(pageSize).Offset((pageNumber - 1) * pageSize);
    }

    /// <summary>
    /// Projects a select query to a different result type while preserving the query structure.
    /// </summary>
    /// <typeparam name="T">The original entity type.</typeparam>
    /// <typeparam name="TResult">The new target projection type.</typeparam>
    /// <param name="query">The query to project.</param>
    /// <returns>A new <see cref="SelectQuery{TResult}"/> instance.</returns>
    public static SelectQuery<TResult> ProjectTo<T, TResult>(this SelectQuery<T> query) where T : class, new() where TResult : class, new()
    {
        var resultQuery = new SelectQuery<TResult>();
        foreach (var node in ((IAstQuery)query).Nodes)
        {
            resultQuery = resultQuery.AddNode(node);
        }
        return resultQuery;
    }

    /// <summary>
    /// Executes the select query asynchronously and returns a standardized <see cref="Result{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the returned items.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">An optional transaction.</param>
    /// <returns>A <see cref="Result{T}"/> containing a read-only list of items, or an error.</returns>
    public static async Task<Result<IReadOnlyList<T>>> ToResultAsync<T>(
        this SelectQuery<T> query,
        IDbConnection connection,
        IDbTransaction? transaction = null) where T : class, new()
    {
        try
        {
            var items = await connection.QueryAsync<T>(query, transaction).ConfigureAwait(false);
            return Success<IReadOnlyList<T>>(items.ToList());
        }
        catch (Exception ex)
        {
            return Failure<IReadOnlyList<T>>(Error.Unexpected("DbError", ex.Message));
        }
    }

    /// <summary>
    /// Executes the select query asynchronously and returns an unbuffered asynchronous stream of items.
    /// </summary>
    /// <typeparam name="T">The type of the returned items.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="connection">The database connection.</param>
    /// <param name="transaction">An optional transaction.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An asynchronous stream of items.</returns>
    public static IAsyncEnumerable<T> ToStreamAsync<T>(
        this SelectQuery<T> query,
        IDbConnection connection,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default) where T : class, new()
    {
        return connection.QueryStreamAsync<T>(query, transaction, cancellationToken);
    }

    /// <summary>
    /// Executes a paginated query asynchronously, including a total count calculation, and returns a <see cref="Result{T}"/> with <see cref="IPagedList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the returned items.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="connection">The database connection.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="transaction">An optional transaction.</param>
    /// <returns>A <see cref="Result{T}"/> containing the paginated list, or an error.</returns>
    public static async Task<Result<IPagedList<T>>> ToPagedListAsync<T>(
        this SelectQuery<T> query,
        IDbConnection connection,
        int pageNumber,
        int pageSize,
        IDbTransaction? transaction = null) where T : class, new()
    {
        try
        {
            var paginatedQuery = query.Paginate(pageNumber, pageSize);
            var items = (await connection.QueryAsync<T>(paginatedQuery, transaction).ConfigureAwait(false)).ToList();

            // Build count query from base query nodes (excluding order by, limit, offset, pagination)
            var ast = (IAstQuery)query;
            var countNodes = ast.Nodes.Where(n => n is not (LimitOffsetNode or OrderByNode or ThenByNode or RawOrderByNode)).ToList();
            
            var countQuery = new SelectQuery<T>();
            foreach (var n in countNodes)
            {
                if (n is not (SelectNode or ExpressionSelectNode or RawSelectNode))
                {
                    countQuery = countQuery.AddNode(n);
                }
            }
            countQuery = countQuery.RawSelect($"COUNT(*)");

            var totalCountItems = await connection.QueryAsync<int>(countQuery, transaction).ConfigureAwait(false);
            int totalCount = totalCountItems.ToList()[0];

            return Success<IPagedList<T>>(PagedList<T>.WithCount(items, PaginationParameters.Create(pageNumber, pageSize), totalCount));
        }
        catch (Exception ex)
        {
            return Failure<IPagedList<T>>(Error.Unexpected("DbError", ex.Message));
        }
    }
}
