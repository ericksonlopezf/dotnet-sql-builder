// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Defines a native strategy for performing bulk insert operations against a specific database provider.
/// </summary>
/// <remarks>
/// Implement this interface to provide a native bulk mechanism (e.g., <c>SqlBulkCopy</c>, <c>NpgsqlBinaryImporter</c>)
/// as an alternative to parameterized batch inserts. Register instances via
/// <see cref="DapperExtensions.RegisterBulkStrategy"/>.
/// </remarks>
public interface IBulkStrategy
{
    /// <summary>
    /// Determines whether this strategy can handle the specified connection.
    /// </summary>
    /// <param name="connection">The active database connection to evaluate.</param>
    /// <returns><see langword="true"/> if this strategy supports the connection type; otherwise, <see langword="false"/>.</returns>
    bool CanHandle(IDbConnection connection);

    /// <summary>
    /// Asynchronously inserts a collection of entities using a native bulk mechanism.
    /// </summary>
    /// <typeparam name="T">The entity type to insert. Must have a default constructor and be a reference type.</typeparam>
    /// <param name="connection">The database connection to use for the bulk insert.</param>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the total number of rows inserted.
    /// </returns>
    Task<int> BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where T : class, new();
}





