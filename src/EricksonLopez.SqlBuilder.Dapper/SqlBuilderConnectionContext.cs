// Copyright © Erickson Lopez. MIT License.
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
/// Encapsulates a database connection with its default SQL compiler.
/// </summary>
public readonly struct SqlBuilderConnectionContext
{
    private readonly IDbConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlBuilderConnectionContext"/> struct.
    /// </summary>
    /// <param name="connection">The database connection to bind to this context.</param>
    public SqlBuilderConnectionContext(IDbConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Starts a SELECT query for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A bound select query.</returns>
    public BoundSelectQuery<T> Select<T>() where T : class, new() => new BoundSelectQuery<T>(EricksonLopez.SqlBuilder.Sql.From<T>(), _connection);

    /// <summary>
    /// Creates an INSERT query for the specified entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance.</returns>
    public InsertQuery<T> Insert<T>(T entity) where T : class, new() => EricksonLopez.SqlBuilder.Sql.Insert(entity);

    /// <summary>
    /// Creates an UPDATE query builder for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>An update builder instance.</returns>
    public EricksonLopez.SqlBuilder.Abstractions.IUpdateSetBuilder<T> Update<T>() where T : class, new() => EricksonLopez.SqlBuilder.Sql.Update<T>();

    /// <summary>
    /// Creates an UPDATE query builder initialized with the values of the specified entity.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="entity">The entity containing modified values.</param>
    /// <returns>An update builder instance.</returns>
    public EricksonLopez.SqlBuilder.Abstractions.IUpdateSetBuilder<T> Update<T>(T entity) where T : class, new() => EricksonLopez.SqlBuilder.Sql.Update<T>(entity);

    /// <summary>
    /// Creates a DELETE query builder for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A delete builder instance.</returns>
    public EricksonLopez.SqlBuilder.Abstractions.IDeleteFromBuilder<T> Delete<T>() where T : class, new() => EricksonLopez.SqlBuilder.Sql.Delete<T>();

    /// <summary>
    /// Gets the underlying database connection.
    /// </summary>
    public IDbConnection Connection => _connection;
}
