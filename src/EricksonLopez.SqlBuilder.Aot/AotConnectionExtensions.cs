// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Aot;

/// <summary>
/// Provides NativeAOT-compatible extension methods on <see cref="IDbConnection"/> that
/// compile SQL builder queries and execute them in a single fluent call, with no reflection.
/// </summary>
/// <remarks>
/// <para>
/// All methods accept a <c>mapper</c> delegate obtained from the source-generated
/// <c>GetReaderParser()</c> method on any <c>[SqlEntity]</c> type:
/// <code>
/// var results = await connection.AotQueryAsync(
///     Sql.From&lt;Order&gt;().Where(o => o.CustomerId == customerId),
///     compiler,
///     Order.GetReaderParser());
/// </code>
/// </para>
/// <para>
/// This differs from the Dapper-based execution path in that it operates purely over ADO.NET
/// primitives, making it safe for NativeAOT, Blazor WASM, and other trim-unfriendly runtimes.
/// </para>
/// </remarks>
[RequiresDynamicCode("Compiling ISqlQuery uses dynamic code generation when evaluating typed LINQ expressions. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
[RequiresUnreferencedCode("Compiling ISqlQuery accesses member metadata that may be trimmed. Use pre-compiled SqlResult overloads for strict NativeAOT paths.")]
public static class AotConnectionExtensions
{
    // ─── Compile + Query ──────────────────────────────────────────────────────

    /// <summary>
    /// Compiles and executes the query, automatically resolving the source-generated <c>IDataReader</c> parser
    /// from <typeparamref name="T"/> with zero reflection.
    /// </summary>
    /// <typeparam name="T">The entity type implementing <see cref="EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="connection">An open <see cref="IDbConnection"/>.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="compiler">The <see cref="ISqlCompiler"/> for the target provider.</param>
    /// <param name="transaction">An optional <see cref="IDbTransaction"/>.</param>
    /// <param name="commandTimeout">Command timeout in seconds (default: 30).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>All matched entities mapped via source-generated parser.</returns>
    public static Task<IReadOnlyList<T>> AotQueryAsync<T>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
        where T : class, EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T>, new()
    {
        return connection.AotQueryAsync<T>(query, compiler, T.GetReaderParser(), transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Compiles the query and executes it, returning all results mapped via the source-generated
    /// <paramref name="mapper"/>. No reflection is used.
    /// </summary>
    /// <typeparam name="T">The entity type to map.</typeparam>
    /// <param name="connection">An open <see cref="IDbConnection"/>.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="compiler">The <see cref="ISqlCompiler"/> for the target provider.</param>
    /// <param name="mapper">
    /// A factory function that maps one <see cref="IDataReader"/> row to an entity.
    /// Obtain via <c>MyEntity.GetReaderParser()</c> (source-generated).
    /// </param>
    /// <param name="transaction">An optional <see cref="IDbTransaction"/>.</param>
    /// <param name="commandTimeout">Command timeout in seconds (default: 30).</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>All matched entities.</returns>
    public static Task<IReadOnlyList<T>> AotQueryAsync<T>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        Func<IDataReader, T> mapper,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(compiler);

        var result = compiler.Compile(query);
        return AotQueryExecutor.QueryAsync(connection, result, mapper, transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Compiles and executes the query, automatically resolving the source-generated parser,
    /// returning the first match or <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">An open database connection.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="compiler">The SQL compiler for the target provider.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The first matched entity, or <see langword="null"/> if empty.</returns>
    public static Task<T?> AotQueryFirstOrDefaultAsync<T>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
        where T : class, EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T>, new()
    {
        return connection.AotQueryFirstOrDefaultAsync<T>(query, compiler, T.GetReaderParser(), transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Compiles and executes the query, returning the first match or <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">An open database connection.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="compiler">The SQL compiler for the target provider.</param>
    /// <param name="mapper">The row mapper function.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The first matched entity, or <see langword="null"/> if empty.</returns>
    public static Task<T?> AotQueryFirstOrDefaultAsync<T>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        Func<IDataReader, T> mapper,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(compiler);

        var result = compiler.Compile(query);
        return AotQueryExecutor.QueryFirstOrDefaultAsync(connection, result, mapper, transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Compiles and executes the query, automatically resolving the source-generated parser,
    /// returning exactly one result or throwing.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">An open database connection.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="compiler">The SQL compiler for the target provider.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The single matched entity.</returns>
    public static Task<T> AotQuerySingleAsync<T>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
        where T : class, EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T>, new()
    {
        return connection.AotQuerySingleAsync<T>(query, compiler, T.GetReaderParser(), transaction, commandTimeout, cancellationToken);
    }

    /// <summary>
    /// Compiles and executes the query, returning exactly one result or throwing.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">An open database connection.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="compiler">The SQL compiler for the target provider.</param>
    /// <param name="mapper">The row mapper function.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The single matched entity.</returns>
    public static Task<T> AotQuerySingleAsync<T>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        Func<IDataReader, T> mapper,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(compiler);

        var result = compiler.Compile(query);
        return AotQueryExecutor.QuerySingleAsync(connection, result, mapper, transaction, commandTimeout, cancellationToken);
    }

    // ─── Compile + Execute (non-query) ────────────────────────────────────────

    /// <summary>
    /// Compiles and executes a non-query statement (INSERT, UPDATE, DELETE), returning rows affected.
    /// </summary>
    /// <param name="connection">An open <see cref="IDbConnection"/>.</param>
    /// <param name="query">The query to compile and execute.</param>
    /// <param name="compiler">The SQL compiler for the target provider.</param>
    /// <param name="transaction">An optional <see cref="IDbTransaction"/>.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public static Task<int> AotExecuteAsync(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(compiler);

        var result = compiler.Compile(query);
        return AotQueryExecutor.ExecuteAsync(connection, result, transaction, commandTimeout, cancellationToken);
    }

    // ─── Compile + Scalar ─────────────────────────────────────────────────────

    /// <summary>
    /// Compiles the query and returns a scalar value (first column, first row).
    /// </summary>
    /// <typeparam name="TScalar">The scalar result type.</typeparam>
    /// <param name="connection">An open database connection.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="compiler">The SQL compiler for the target provider.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The scalar result value, or <see langword="default"/> if empty.</returns>
    public static Task<TScalar?> AotQueryScalarAsync<TScalar>(
        this IDbConnection connection,
        ISqlQuery query,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(compiler);

        var result = compiler.Compile(query);
        return AotQueryExecutor.QueryScalarAsync<TScalar>(connection, result, transaction, commandTimeout, cancellationToken);
    }

    // ─── Pre-compiled (SqlResult) overloads ───────────────────────────────────

    /// <summary>
    /// Executes a pre-compiled <see cref="SqlResult"/> without re-compiling, automatically resolving the source-generated parser.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">An open database connection.</param>
    /// <param name="result">The pre-compiled SQL result.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>All matched entities.</returns>
    public static Task<IReadOnlyList<T>> AotQueryAsync<T>(
        this IDbConnection connection,
        SqlResult result,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
        where T : class, EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T>, new()
        => AotQueryExecutor.QueryAsync(connection, result, T.GetReaderParser(), transaction, commandTimeout, cancellationToken);

    /// <summary>
    /// Executes a pre-compiled <see cref="SqlResult"/> without re-compiling, automatically resolving the source-generated parser,
    /// returning the first match or <see langword="null"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">An open database connection.</param>
    /// <param name="result">The pre-compiled SQL result.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The first matched entity, or <see langword="null"/> if empty.</returns>
    public static Task<T?> AotQueryFirstOrDefaultAsync<T>(
        this IDbConnection connection,
        SqlResult result,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
        where T : class, EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T>, new()
        => AotQueryExecutor.QueryFirstOrDefaultAsync(connection, result, T.GetReaderParser(), transaction, commandTimeout, cancellationToken);

    /// <summary>
    /// Executes a pre-compiled <see cref="SqlResult"/> without re-compiling, automatically resolving the source-generated parser,
    /// returning exactly one result or throwing.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">An open database connection.</param>
    /// <param name="result">The pre-compiled SQL result.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The single matched entity.</returns>
    public static Task<T> AotQuerySingleAsync<T>(
        this IDbConnection connection,
        SqlResult result,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
        where T : class, EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T>, new()
        => AotQueryExecutor.QuerySingleAsync(connection, result, T.GetReaderParser(), transaction, commandTimeout, cancellationToken);

    /// <summary>
    /// Executes a pre-compiled <see cref="SqlResult"/> without re-compiling — useful when the
    /// same compiled SQL is reused multiple times.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">An open database connection.</param>
    /// <param name="result">The pre-compiled SQL result.</param>
    /// <param name="mapper">The row mapper function.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>All matched entities.</returns>
    public static Task<IReadOnlyList<T>> AotQueryAsync<T>(
        this IDbConnection connection,
        SqlResult result,
        Func<IDataReader, T> mapper,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
        => AotQueryExecutor.QueryAsync(connection, result, mapper, transaction, commandTimeout, cancellationToken);

    /// <summary>
    /// Executes a pre-compiled <see cref="SqlResult"/> non-query — useful when the
    /// compiled SQL is cached.
    /// </summary>
    /// <param name="connection">An open database connection.</param>
    /// <param name="result">The pre-compiled SQL result.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="commandTimeout">Command timeout in seconds.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The number of rows affected.</returns>
    public static Task<int> AotExecuteAsync(
        this IDbConnection connection,
        SqlResult result,
        IDbTransaction? transaction = null,
        int commandTimeout = 30,
        CancellationToken cancellationToken = default)
        => AotQueryExecutor.ExecuteAsync(connection, result, transaction, commandTimeout, cancellationToken);
}





