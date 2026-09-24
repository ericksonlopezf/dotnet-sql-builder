// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Provides multi-mapping extension methods for <see cref="IDbConnection"/>,
/// allowing split-on multi-entity mapping using <see cref="ISqlQuery"/> objects.
/// </summary>
/// <remarks>
/// These wrappers cover Dapper's built-in 2-7 entity overloads.
/// Cancellation support is provided by wrapping the call in a task and observing the token.
/// </remarks>
public static class DapperMultiMappingExtensions
{
    // ──────────────────────────────────────────────────────────────────────────
    // 2-entity mapping: TFirst + TSecond → TReturn
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously executes a JOIN query and maps results to two entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the two entities into the aggregate return type.</param>
    /// <param name="splitOn">The column name that marks the start of the second entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of <typeparamref name="TReturn"/> instances.</returns>
    public static async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return await connection.QueryAsync(result.Sql, map, dynamicParams, transaction, buffered, splitOn).ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronously executes a JOIN query and maps results to two entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the two entities into the aggregate return type.</param>
    /// <param name="splitOn">The column name that marks the start of the second entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <returns>A collection of <typeparamref name="TReturn"/> instances resulting from the multi-entity mapping.</returns>
    public static IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true)
    {
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return connection.Query(result.Sql, map, dynamicParams, transaction, buffered, splitOn);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3-entity mapping
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously executes a JOIN query and maps results to three entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the three entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of <typeparamref name="TReturn"/> instances.</returns>
    public static async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return await connection.QueryAsync(result.Sql, map, dynamicParams, transaction, buffered, splitOn).ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronously executes a JOIN query and maps results to three entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the three entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <returns>A collection of <typeparamref name="TReturn"/> instances resulting from the multi-entity mapping.</returns>
    public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true)
    {
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return connection.Query(result.Sql, map, dynamicParams, transaction, buffered, splitOn);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 4-entity mapping
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously executes a JOIN query and maps results to four entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TFourth">The fourth entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the four entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of <typeparamref name="TReturn"/> instances.</returns>
    public static async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TFourth, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return await connection.QueryAsync(result.Sql, map, dynamicParams, transaction, buffered, splitOn).ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronously executes a JOIN query and maps results to four entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TFourth">The fourth entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the four entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <returns>A collection of <typeparamref name="TReturn"/> instances resulting from the multi-entity mapping.</returns>
    public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TFourth, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true)
    {
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return connection.Query(result.Sql, map, dynamicParams, transaction, buffered, splitOn);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 5-entity mapping
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously executes a JOIN query and maps results to five entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TFourth">The fourth entity type.</typeparam>
    /// <typeparam name="TFifth">The fifth entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the five entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of <typeparamref name="TReturn"/> instances.</returns>
    public static async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return await connection.QueryAsync(result.Sql, map, dynamicParams, transaction, buffered, splitOn).ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronously executes a JOIN query and maps results to five entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TFourth">The fourth entity type.</typeparam>
    /// <typeparam name="TFifth">The fifth entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the five entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <returns>A collection of <typeparamref name="TReturn"/> instances resulting from the multi-entity mapping.</returns>
    public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true)
    {
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return connection.Query(result.Sql, map, dynamicParams, transaction, buffered, splitOn);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 6-entity mapping
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously executes a JOIN query and maps results to six entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TFourth">The fourth entity type.</typeparam>
    /// <typeparam name="TFifth">The fifth entity type.</typeparam>
    /// <typeparam name="TSixth">The sixth entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the six entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of <typeparamref name="TReturn"/> instances.</returns>
    public static async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return await connection.QueryAsync(result.Sql, map, dynamicParams, transaction, buffered, splitOn).ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronously executes a JOIN query and maps results to six entity types.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TFourth">The fourth entity type.</typeparam>
    /// <typeparam name="TFifth">The fifth entity type.</typeparam>
    /// <typeparam name="TSixth">The sixth entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the six entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <returns>A collection of <typeparamref name="TReturn"/> instances resulting from the multi-entity mapping.</returns>
    public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true)
    {
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return connection.Query(result.Sql, map, dynamicParams, transaction, buffered, splitOn);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 7-entity mapping (Dapper's maximum typed overload)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously executes a JOIN query and maps results to seven entity types.
    /// This is the maximum typed overload supported by Dapper.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TFourth">The fourth entity type.</typeparam>
    /// <typeparam name="TFifth">The fifth entity type.</typeparam>
    /// <typeparam name="TSixth">The sixth entity type.</typeparam>
    /// <typeparam name="TSeventh">The seventh entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the seven entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a collection of <typeparamref name="TReturn"/> instances.</returns>
    public static async Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return await connection.QueryAsync(result.Sql, map, dynamicParams, transaction, buffered, splitOn).ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronously executes a JOIN query and maps results to seven entity types.
    /// This is the maximum typed overload supported by Dapper.
    /// </summary>
    /// <typeparam name="TFirst">The first entity type representing the left side of the split.</typeparam>
    /// <typeparam name="TSecond">The second entity type.</typeparam>
    /// <typeparam name="TThird">The third entity type.</typeparam>
    /// <typeparam name="TFourth">The fourth entity type.</typeparam>
    /// <typeparam name="TFifth">The fifth entity type.</typeparam>
    /// <typeparam name="TSixth">The sixth entity type.</typeparam>
    /// <typeparam name="TSeventh">The seventh entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection to execute the query against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the seven entities into the aggregate return type.</param>
    /// <param name="splitOn">The column names that mark the start of each subsequent entity.</param>
    /// <param name="transaction">An optional database transaction to participate in.</param>
    /// <param name="buffered">A value indicating whether to buffer the results in memory before returning.</param>
    /// <returns>A collection of <typeparamref name="TReturn"/> instances resulting from the multi-entity mapping.</returns>
    public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
        this IDbConnection connection,
        ISqlQuery query,
        Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map,
        string splitOn = "Id",
        IDbTransaction? transaction = null,
        bool buffered = true)
    {
        var compiler = DapperExtensions.GetCompiler(connection);
        var result = query.Build(compiler);
        var dynamicParams = result.ToDynamicParameters();
        return connection.Query(result.Sql, map, dynamicParams, transaction, buffered, splitOn);
    }
}




