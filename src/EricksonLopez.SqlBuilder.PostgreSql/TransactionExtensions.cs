// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Result;
using Npgsql;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Provides extension methods for executing operations within a PostgreSQL transaction,
/// supporting the Unit of Work pattern without requiring a dedicated UoW class.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Requires live PostgreSQL; covered by integration tests.")]
public static class TransactionExtensions
{
    /// <summary>
    /// Executes an async operation within a new transaction.
    /// Commits on success. Rolls back on any exception (including cancellation).
    /// </summary>
    /// <param name="connection">The PostgreSQL database connection.</param>
    /// <param name="operation">The asynchronous operation to execute within the transaction.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous transaction operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="operation"/> is <see langword="null"/></exception>
    public static async Task ExecuteInTransactionAsync(
        this NpgsqlConnection connection,
        Func<NpgsqlTransaction, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(operation);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await operation(transaction).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Executes an async operation within a new transaction and returns a result.
    /// Commits on success. Rolls back on any exception.
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <param name="connection">The PostgreSQL database connection.</param>
    /// <param name="operation">The asynchronous operation to execute within the transaction.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result returned by <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="operation"/> is <see langword="null"/></exception>
    public static async Task<TResult> ExecuteInTransactionAsync<TResult>(
        this NpgsqlConnection connection,
        Func<NpgsqlTransaction, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(operation);

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await operation(transaction).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            // Stryker disable once block : Justification: Re-throw required to preserve stack trace after rollback
            throw;
        }
    }
}




