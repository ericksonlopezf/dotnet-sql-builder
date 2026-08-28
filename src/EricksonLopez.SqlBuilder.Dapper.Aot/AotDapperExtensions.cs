// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Aot;

namespace EricksonLopez.SqlBuilder.Dapper.Aot;

/// <summary>
/// Provides extension methods on <see cref="IDbConnection"/> for zero-reflection, NativeAOT-compatible SQL execution
/// seamlessly integrated with source-generated entity parsers and pre-compiled <see cref="SqlResult"/>.
/// </summary>
public static class AotDapperExtensions
{
    /// <summary>
    /// Executes a pre-compiled query asynchronously without reflection, mapping rows to <typeparamref name="T"/> via the provided reader parser.
    /// </summary>
    /// <typeparam name="T">The result entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="result">The pre-compiled SQL result.</param>
    /// <param name="mapper">The reader parser delegate.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A read-only list of mapped <typeparamref name="T"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/>, <paramref name="result"/>, or <paramref name="mapper"/> is <see langword="null"/></exception>
    public static Task<IReadOnlyList<T>> AotQueryAsync<T>(
        this IDbConnection connection,
        SqlResult result,
        Func<IDataReader, T> mapper,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(mapper);

        return AotQueryExecutor.QueryAsync(connection, result, mapper, transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Executes a pre-compiled query asynchronously without reflection, returning the first row mapped to <typeparamref name="T"/> or default.
    /// </summary>
    /// <typeparam name="T">The result entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="result">The pre-compiled SQL result.</param>
    /// <param name="mapper">The reader parser delegate.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The first mapped <typeparamref name="T"/> instance, or default if empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/>, <paramref name="result"/>, or <paramref name="mapper"/> is <see langword="null"/></exception>
    public static async Task<T?> AotQueryFirstOrDefaultAsync<T>(
        this IDbConnection connection,
        SqlResult result,
        Func<IDataReader, T> mapper,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        var list = await AotQueryAsync(connection, result, mapper, transaction, commandTimeout, cancellationToken).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : default;
    }

    /// <summary>
    /// Executes a pre-compiled non-query command (INSERT, UPDATE, DELETE) asynchronously without reflection.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="result">The pre-compiled SQL result.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="result"/> is <see langword="null"/></exception>
    public static Task<int> AotExecuteAsync(
        this IDbConnection connection,
        SqlResult result,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(result);

        return AotQueryExecutor.ExecuteAsync(connection, result, transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Executes a pre-compiled query and returns a single scalar value asynchronously without reflection.
    /// </summary>
    /// <typeparam name="T">The scalar return type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="result">The pre-compiled SQL result.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The scalar result converted to <typeparamref name="T"/>, or default.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="result"/> is <see langword="null"/></exception>
    public static Task<T?> AotExecuteScalarAsync<T>(
        this IDbConnection connection,
        SqlResult result,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(result);

        return AotQueryExecutor.QueryScalarAsync<T>(connection, result, transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Compiles and executes a query asynchronously without reflection, mapping rows to <typeparamref name="T"/> via the provided reader parser.
    /// </summary>
    /// <typeparam name="T">The result entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="compiler">The dialect compiler.</param>
    /// <param name="mapper">The reader parser delegate.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A read-only list of mapped <typeparamref name="T"/> instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/>, <paramref name="query"/>, <paramref name="compiler"/>, or <paramref name="mapper"/> is <see langword="null"/></exception>
    [RequiresDynamicCode("Compiling ISqlQuery uses dynamic code generation when evaluating typed LINQ expressions. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
    [RequiresUnreferencedCode("Compiling ISqlQuery accesses member metadata that may be trimmed. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
    public static Task<IReadOnlyList<T>> AotQueryAsync<T>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        Func<IDataReader, T> mapper,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(compiler);
        ArgumentNullException.ThrowIfNull(mapper);

        var compiled = compiler.Compile(query);
        return AotQueryExecutor.QueryAsync(connection, compiled, mapper, transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Compiles and executes a query asynchronously without reflection, returning the first row mapped to <typeparamref name="T"/> or default.
    /// </summary>
    /// <typeparam name="T">The result entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="compiler">The dialect compiler.</param>
    /// <param name="mapper">The reader parser delegate.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The first mapped <typeparamref name="T"/> instance, or default if empty.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/>, <paramref name="query"/>, <paramref name="compiler"/>, or <paramref name="mapper"/> is <see langword="null"/></exception>
    [RequiresDynamicCode("Compiling ISqlQuery uses dynamic code generation when evaluating typed LINQ expressions. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
    [RequiresUnreferencedCode("Compiling ISqlQuery accesses member metadata that may be trimmed. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
    public static async Task<T?> AotQueryFirstOrDefaultAsync<T>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        Func<IDataReader, T> mapper,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        var list = await AotQueryAsync(connection, query, compiler, mapper, transaction, commandTimeout, cancellationToken).ConfigureAwait(false);
        return list.Count > 0 ? list[0] : default;
    }

    /// <summary>
    /// Compiles and executes a non-query command (INSERT, UPDATE, DELETE) asynchronously without reflection.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="compiler">The dialect compiler.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/>, <paramref name="query"/>, or <paramref name="compiler"/> is <see langword="null"/></exception>
    [RequiresDynamicCode("Compiling ISqlQuery uses dynamic code generation when evaluating typed LINQ expressions. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
    [RequiresUnreferencedCode("Compiling ISqlQuery accesses member metadata that may be trimmed. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
    public static Task<int> AotExecuteAsync(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(compiler);

        var compiled = compiler.Compile(query);
        return AotQueryExecutor.ExecuteAsync(connection, compiled, transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Compiles and executes a query and returns a single scalar value asynchronously without reflection.
    /// </summary>
    /// <typeparam name="T">The scalar return type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="compiler">The dialect compiler.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">The command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The scalar result converted to <typeparamref name="T"/>, or default.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/>, <paramref name="query"/>, or <paramref name="compiler"/> is <see langword="null"/></exception>
    [RequiresDynamicCode("Compiling ISqlQuery uses dynamic code generation when evaluating typed LINQ expressions. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
    [RequiresUnreferencedCode("Compiling ISqlQuery accesses member metadata that may be trimmed. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
    public static Task<T?> AotExecuteScalarAsync<T>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(compiler);

        var compiled = compiler.Compile(query);
        return AotQueryExecutor.QueryScalarAsync<T>(connection, compiled, transaction, commandTimeout, cancellationToken);
    }
}
