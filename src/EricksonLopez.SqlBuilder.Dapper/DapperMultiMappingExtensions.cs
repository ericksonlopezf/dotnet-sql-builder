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
/// <para>
/// These wrappers cover Dapper's built-in 2-7 entity overloads.
/// Cancellation support is provided by wrapping the call in a Task and observing the token.
/// </para>
/// </summary>
public static class DapperMultiMappingExtensions
{
    // ──────────────────────────────────────────────────────────────────────────
    // 2-entity mapping: TFirst + TSecond → TReturn
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously executes a JOIN query and maps results to two entity types.
    /// </summary>
    /// <typeparam name="TFirst">First entity type (left side of split).</typeparam>
    /// <typeparam name="TSecond">Second entity type.</typeparam>
    /// <typeparam name="TReturn">The returned aggregate type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="map">The mapping function that combines the two entities.</param>
    /// <param name="splitOn">The column name that marks the start of the second entity.</param>
    /// <param name="transaction">An optional transaction.</param>
    /// <param name="buffered">Whether to buffer the results in memory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of <typeparamref name="TReturn"/> instances.</returns>
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




