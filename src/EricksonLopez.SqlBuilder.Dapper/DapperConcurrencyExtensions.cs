// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Extension methods for optimistic concurrency execution with Dapper.
/// </summary>
public static class DapperConcurrencyExtensions
{
    /// <summary>
    /// Executes an UPDATE query compiled with <c>WithConcurrencyToken</c> and throws
    /// <see cref="EricksonLopez.SqlBuilder.Abstractions.DbConcurrencyException"/> if the update affects zero rows.
    /// </summary>
    /// <typeparam name="T">The entity type being updated.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="query">The UPDATE query. Must include a <c>WithConcurrencyToken</c> node.</param>
    /// <param name="compiler">The SQL compiler for the target dialect.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds.</param>
    /// <returns>The number of rows affected (always &gt;= 1 on success).</returns>
    /// <exception cref="EricksonLopez.SqlBuilder.Abstractions.DbConcurrencyException">Zero rows are affected, indicating a concurrency conflict</exception>
    /// <example>
    /// <code>
    /// await connection.ExecuteWithConcurrencyCheckAsync&lt;User&gt;(
    ///     Sql.Update&lt;User&gt;()
    ///        .Set(u => u.Name, "Alice")
    ///        .Where(u => u.Id == userId)
    ///        .WithConcurrencyToken(u => u.Version, currentVersion),
    ///     sqlServerCompiler);
    /// </code>
    /// </example>
    public static async Task<int> ExecuteWithConcurrencyCheckAsync<T>(
        this IDbConnection connection,
        EricksonLopez.SqlBuilder.Abstractions.ISqlQuery query,
        ISqlCompiler compiler,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
        where T : class
    {
        var result = query.Build(compiler);
        var rowsAffected = await connection.ExecuteAsync(
            result.Sql,
            result.Parameters,
            transaction,
            commandTimeout).ConfigureAwait(false);

        if (rowsAffected == 0)
        {
            throw new EricksonLopez.SqlBuilder.Abstractions.DbConcurrencyException(typeof(T).Name, rowsAffected);
        }

        return rowsAffected;
    }

    /// <summary>
    /// Executes an UPDATE query compiled with <c>WithConcurrencyToken</c> and throws
    /// <see cref="EricksonLopez.SqlBuilder.Abstractions.DbConcurrencyException"/> if the update affects zero rows.
    /// Uses the connection's registered compiler automatically.
    /// </summary>
    /// <typeparam name="T">The entity type being updated.</typeparam>
    /// <param name="connection">The database connection (must have a registered compiler via <c>DapperExtensions.RegisterCompiler</c>).</param>
    /// <param name="query">The UPDATE query including a <c>WithConcurrencyToken</c> node.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds.</param>
    /// <returns>The number of rows affected (always &gt;= 1 on success).</returns>
    /// <exception cref="EricksonLopez.SqlBuilder.Abstractions.DbConcurrencyException">Zero rows are affected, indicating a concurrency conflict</exception>
    public static Task<int> ExecuteWithConcurrencyCheckAsync<T>(
        this IDbConnection connection,
        EricksonLopez.SqlBuilder.Abstractions.ISqlQuery query,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
        where T : class
    {
        var compiler = DapperExtensions.GetCompiler(connection);
        return connection.ExecuteWithConcurrencyCheckAsync<T>(query, compiler, transaction, commandTimeout);
    }
}
