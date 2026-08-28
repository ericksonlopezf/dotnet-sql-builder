// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.SqlBuilder;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Provides extension methods on <see cref="System.Data.IDbConnection"/> for executing paginated SQL queries.
/// </summary>
public static class DapperPaginationExtensions
{
    /// <summary>
    /// Executes a <see cref="SelectQuery{T}"/> with pagination, returning a <see cref="PagedList{T}"/>.
    /// If <paramref name="countTotal"/> is true, it will execute a separate COUNT query first.
    /// </summary>
    /// <typeparam name="T">The type of the returned items.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="query">The base select query to paginate.</param>
    /// <param name="parameters">The pagination parameters (page number, page size).</param>
    /// <param name="countTotal">If <see langword="true"/>, executes a COUNT query to populate total count metadata.</param>
    /// <param name="transaction">An optional transaction.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a <see cref="IPagedList{T}"/> with the requested page and optional total count.</returns>
    public static async Task<IPagedList<T>> QueryPagedAsync<T>(
        this IDbConnection connection,
        SelectQuery<T> query,
        PaginationParameters parameters,
        bool countTotal = true,
        IDbTransaction? transaction = null) where T : class, new()
    {
        if (countTotal)
        {
            var countNodes = query.Nodes.Where(n => n switch
            {
                SelectNode => false,
                ExpressionSelectNode => false,
                RawSelectNode => false,
                OrderByNode => false,
                RawOrderByNode => false,
                LimitOffsetNode => false,
                _ => true
            });

            var countQuery = query with { Nodes = ImmutableArray.CreateRange(countNodes) };
            countQuery = countQuery.RawSelect($"COUNT(*)");
            
            var count = await connection.QueryFirstOrDefaultAotAsync<int>(
                countQuery, 
                reader => reader.GetInt32(0), 
                transaction).ConfigureAwait(false);

            if (count == 0)
            {
                return PagedList<T>.Empty(parameters);
            }

            // Apply pagination to the original query
            var offset = (parameters.Page - 1) * parameters.PageSize;
            var pagedQuery = query.Limit(parameters.PageSize).Offset(offset);
            
            var items = await connection.QueryAsync<T>(pagedQuery, transaction).ConfigureAwait(false);

            return PagedList<T>.WithCount(items.ToList(), parameters, count);
        }
        else
        {
            // Fetch PageSize + 1 to check if there is a next page
            var fetchSize = parameters.PageSize + 1;
            var offset = (parameters.Page - 1) * parameters.PageSize;

            var pagedQuery = query.Limit(fetchSize).Offset(offset);

            var items = (await connection.QueryAsync<T>(pagedQuery, transaction).ConfigureAwait(false)).ToList();

            bool hasNextPage = items.Count > parameters.PageSize;
            if (hasNextPage)
            {
                items.RemoveAt(parameters.PageSize);
            }

            return PagedList<T>.WithoutCount(items, parameters, hasNextPage);
        }
    }

    /// <summary>
    /// Executes a paginated query using raw SQL and returns a <see cref="PagedList{T}"/>.
    /// Executes two statements: one for the page data (with LIMIT/OFFSET appended automatically)
    /// and one for the total count.
    /// </summary>
    /// <typeparam name="T">The mapped result type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">
    /// The data query. Do NOT include LIMIT/OFFSET — they are appended automatically
    /// based on <paramref name="parameters"/>.
    /// </param>
    /// <param name="countSql">
    /// The count query. Should return a single <c>int</c> or <c>long</c>.
    /// Example: <c>SELECT COUNT(*) FROM products WHERE active = true</c>
    /// </param>
    /// <param name="parameters">Pagination parameters (page number, page size).</param>
    /// <param name="param">Optional Dapper parameters shared between both queries.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds.</param>
    /// <returns>A <see cref="PagedList{T}"/> with items and full pagination metadata.</returns>
    /// <exception cref="ArgumentException"><paramref name="sql"/> or <paramref name="countSql"/> is null, empty, or whitespace</exception>
    /// <remarks>
    /// Usage:
    /// <code>
    /// var page = await connection.QueryPagedRawAsync&lt;ProductDto&gt;(
    ///     sql:        "SELECT id, name, price FROM products WHERE active = @Active ORDER BY name",
    ///     countSql:   "SELECT COUNT(*) FROM products WHERE active = @Active",
    ///     parameters: PaginationParameters.Create(page: 1, pageSize: 20),
    ///     param:      new { Active = true });
    /// </code>
    /// </remarks>
    public static async Task<IPagedList<T>> QueryPagedRawAsync<T>(
        this IDbConnection connection,
        string sql,
        string countSql,
        PaginationParameters parameters,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentException.ThrowIfNullOrWhiteSpace(countSql);

        var pagedSql = $"{sql} LIMIT {parameters.PageSize} OFFSET {(parameters.Page - 1) * parameters.PageSize}";

        var items = await connection.QueryAsync<T>(pagedSql, param, transaction, commandTimeout)
            .ConfigureAwait(false);

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, param, transaction, commandTimeout)
            .ConfigureAwait(false);

        return PagedList<T>.WithCount(items.ToList(), parameters, totalCount);
    }

    /// <summary>
    /// Executes a paginated query using a single raw SQL batch with multiple result sets,
    /// returning a <see cref="PagedList{T}"/>.
    /// The SQL must return two result sets: the page data first, then <c>COUNT(*)</c> second.
    /// More efficient than <see cref="QueryPagedRawAsync{T}"/> as it uses a single round-trip.
    /// </summary>
    /// <typeparam name="T">The mapped result type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">
    /// A SQL batch with two statements separated by semicolons.
    /// The first must return rows; the second must return a scalar count.
    /// </param>
    /// <param name="parameters">Pagination parameters (page number, page size).</param>
    /// <param name="param">Optional Dapper parameters (LIMIT, OFFSET, filters, etc.).</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds.</param>
    /// <returns>A <see cref="PagedList{T}"/> with items and full pagination metadata.</returns>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is null, empty, or whitespace</exception>
    /// <remarks>
    /// Expected SQL pattern:
    /// <code>
    /// SELECT id, name FROM products WHERE active = @Active ORDER BY name LIMIT @Limit OFFSET @Offset;
    /// SELECT COUNT(*) FROM products WHERE active = @Active;
    /// </code>
    /// </remarks>
    public static async Task<IPagedList<T>> QueryPagedMultipleAsync<T>(
        this IDbConnection connection,
        string sql,
        PaginationParameters parameters,
        object? param = null,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        using var multi = await connection.QueryMultipleAsync(sql, param, transaction, commandTimeout)
            .ConfigureAwait(false);

        var items = (await multi.ReadAsync<T>().ConfigureAwait(false)).ToList();
        var totalCount = await multi.ReadSingleAsync<int>().ConfigureAwait(false);

        return PagedList<T>.WithCount(items, parameters, totalCount);
    }
}







