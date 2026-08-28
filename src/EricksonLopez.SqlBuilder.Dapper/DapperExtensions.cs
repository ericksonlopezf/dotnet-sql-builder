// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Provides extension methods for <see cref="IDbConnection"/> to seamlessly integrate EricksonLopez.SqlBuilder with Dapper.
/// </summary>
public static class DapperExtensions
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<System.Type, ISqlCompiler> _compilerRegistry = new();
    
    /// <summary>
    /// Registers an ISqlCompiler factory for a specific IDbConnection type.
    /// This allows the extension methods to dynamically resolve the correct SQL dialect at runtime.
    /// </summary>
    /// <typeparam name="TConnection">The specific type of IDbConnection (e.g., SqlConnection, NpgsqlConnection).</typeparam>
    /// <param name="compilerFactory">A factory function that returns an instance of the specific ISqlCompiler.</param>
    public static void RegisterCompiler<TConnection>(System.Func<ISqlCompiler> compilerFactory) where TConnection : IDbConnection
    {
        _compilerRegistry[typeof(TConnection)] = compilerFactory();
    }

    /// <summary>
    /// Registers a custom type handler with both <see cref="EricksonLopez.SqlBuilder.Sql"/> and Dapper's <see cref="SqlMapper"/> simultaneously.
    /// </summary>
    /// <typeparam name="T">The type for which the handler is registered.</typeparam>
    /// <param name="handler">The handler implementation responsible for reading and writing values of type <typeparamref name="T"/>.</param>
    public static void RegisterTypeHandler<T>(EricksonLopez.SqlBuilder.Abstractions.ITypeHandler handler)
    {
        Sql.RegisterTypeHandler<T>(handler);
        SqlMapper.AddTypeHandler(typeof(T), new DapperTypeHandlerAdapter(handler));
    }

    private class DapperTypeHandlerAdapter : SqlMapper.ITypeHandler
    {
        private readonly EricksonLopez.SqlBuilder.Abstractions.ITypeHandler _handler;
        public DapperTypeHandlerAdapter(EricksonLopez.SqlBuilder.Abstractions.ITypeHandler handler) => _handler = handler;

        public void SetValue(IDbDataParameter parameter, object value) => _handler.SetValue(parameter, value);
        public object? Parse(System.Type destinationType, object value) => _handler.Parse(destinationType, value);
    }

    /// <summary>
    /// Retrieves the <see cref="ISqlCompiler"/> registered for the specified connection's runtime type.
    /// </summary>
    /// <param name="connection">The active database connection whose concrete type is used to look up the compiler.</param>
    /// <returns>The registered <see cref="ISqlCompiler"/> for the connection type.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// No compiler has been registered for the connection type via <see cref="RegisterCompiler{TConnection}"/>.
    /// </exception>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static ISqlCompiler GetCompiler(IDbConnection connection)
    {
        var type = connection.GetType();
        if (_compilerRegistry.TryGetValue(type, out var compiler))
        {
            return compiler;
        }
        
        throw new System.InvalidOperationException($"No SQL compiler registered for connection type {type.Name}. Please call DapperExtensions.RegisterCompiler first.");
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private readonly struct QueryMetrics : IDisposable
    {
        private readonly Activity? _activity;
        private readonly Stopwatch _stopwatch;
        private readonly string _sql;
        private readonly Microsoft.Extensions.Logging.ILogger? _logger;

        public QueryMetrics(string operation, string sql)
        {
            _sql = sql;
            _activity = SqlBuilderDiagnostics.ActivitySource.StartActivity(operation);
            _activity?.SetTag("db.statement", sql);
            _stopwatch = Stopwatch.StartNew();
            _logger = SqlBuilderDiagnostics.LoggerFactory?.CreateLogger("EricksonLopez.SqlBuilder.Dapper");
            SqlBuilderDiagnostics.QueryExecutionCounter.Add(1);
        }

        public void AddParameter(string key, object? value)
        {
            if (SqlBuilderDiagnostics.LogParameters)
            {
                _activity?.SetTag($"db.parameter.{key}", value?.ToString());
            }
            else
            {
                _activity?.SetTag($"db.parameter.{key}", "***");
            }
        }

        public void SetError(Exception ex)
        {
            _activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            SqlBuilderDiagnostics.ErrorQueryCounter.Add(1);
            if (_logger != null)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(_logger, ex, "Error executing SQL: {Sql}", _sql);
            }
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            double elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;
            _activity?.SetTag("db.duration_ms", elapsedMs);
            _activity?.Dispose();

            SqlBuilderDiagnostics.QueryDurationHistogram.Record(elapsedMs);

            if (elapsedMs > SqlBuilderDiagnostics.SlowQueryThresholdMs)
            {
                SqlBuilderDiagnostics.SlowQueryCounter.Add(1);
                if (_logger != null)
                {
                    Microsoft.Extensions.Logging.LoggerExtensions.LogWarning(_logger, "Slow query detected ({Elapsed}ms): {Sql}", elapsedMs, _sql);
                }
            }
        }
    }

    // --- TELEMETRY RUNNERS ---
    private static async Task<TResult> RunDapperAsync<TResult>(
        IDbConnection connection, 
        ISqlQuery query, 
        string operationName, 
        IDbTransaction? transaction,
        CancellationToken cancellationToken,
        System.Func<CommandDefinition, Task<TResult>> action)
    {
        var compiler = GetCompiler(connection);
        var result = query.Build(compiler);
        
        using var metrics = new QueryMetrics(operationName, result.Sql);
        var dynamicParams = new DynamicParameters();
        
        foreach (var param in result.Parameters)
        {
            dynamicParams.Add(param.Key, param.Value);
            metrics.AddParameter(param.Key, param.Value);
        }
        
        var cmdDef = new CommandDefinition(result.Sql, dynamicParams, transaction, cancellationToken: cancellationToken);
        
        try
        {
            return await action(cmdDef).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            metrics.SetError(ex);
            // Stryker disable once block : Justification: Re-throw required to record metrics
            throw;
        }
    }

    private static TResult RunDapper<TResult>(
        IDbConnection connection, 
        ISqlQuery query, 
        string operationName, 
        System.Func<string, DynamicParameters, TResult> action)
    {
        var compiler = GetCompiler(connection);
        var result = query.Build(compiler);
        
        using var metrics = new QueryMetrics(operationName, result.Sql);
        var dynamicParams = new DynamicParameters();
        
        foreach (var param in result.Parameters)
        {
            dynamicParams.Add(param.Key, param.Value);
            metrics.AddParameter(param.Key, param.Value);
        }
        
        try
        {
            return action(result.Sql, dynamicParams);
        }
        catch (Exception ex)
        {
            metrics.SetError(ex);
            // Stryker disable once block : Justification: Re-throw required to record metrics
            throw;
        }
    }

    private static async Task<TResult> RunCommandAsync<TResult>(
        IDbConnection connection, 
        ISqlQuery query, 
        string operationName, 
        IDbTransaction? transaction,
        System.Func<IDbCommand, Task<TResult>> action,
        CancellationToken cancellationToken = default)
    {
        var compiler = GetCompiler(connection);
        var result = query.Build(compiler);
        
        using var metrics = new QueryMetrics(operationName, result.Sql);
        
        using var cmd = connection.CreateCommand();
        cmd.CommandText = result.Sql;
        cmd.Transaction = transaction;
        
        foreach (var param in result.Parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = param.Key;
            p.Value = param.Value ?? System.DBNull.Value;
            cmd.Parameters.Add(p);
            metrics.AddParameter(param.Key, param.Value);
        }
        
        try
        {
            return await action(cmd).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            metrics.SetError(ex);
            // Stryker disable once block : Justification: Re-throw required to record metrics
            throw;
        }
    }

    private static Task<int> RunBulkAsync<T>(
        IDbConnection connection, 
        ISqlQuery query, 
        string operationName, 
        IEnumerable<T> entities, 
        IDbTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var compiler = GetCompiler(connection);
        var result = query.Build(compiler);
        
        using var metrics = new QueryMetrics(operationName, result.Sql);
        var cmdDef = new CommandDefinition(result.Sql, entities, transaction, cancellationToken: cancellationToken);
        return connection.ExecuteAsync(cmdDef);
    }

    // --- ASYNC METHODS ---
    /// <summary>
    /// Asynchronously executes a query and maps the results to strongly typed objects.
    /// </summary>
    /// <typeparam name="T">The type to map each result row to.</typeparam>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the collection
    /// of mapped <typeparamref name="T"/> instances.
    /// </returns>
    public static Task<IEnumerable<T>> QueryAsync<T>(
        this IDbConnection connection, 
        ISqlQuery query, 
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return RunDapperAsync(connection, query, "db.query", transaction, cancellationToken,
            cmdDef => connection.QueryAsync<T>(cmdDef));
    }
    
    /// <summary>
    /// Asynchronously executes a query and yields results as an unbuffered asynchronous stream.
    /// </summary>
    /// <remarks>
    /// Rows are streamed directly from the database as they become available, making this method
    /// suitable for processing large result sets without buffering all rows in memory.
    /// Requires the connection to be a <see cref="System.Data.Common.DbConnection"/>.
    /// </remarks>
    /// <typeparam name="T">The type to map each result row to.</typeparam>
    /// <param name="connection">The database connection to execute against. Must derive from <see cref="System.Data.Common.DbConnection"/>.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous enumeration.</param>
    /// <returns>An asynchronous stream of <typeparamref name="T"/> instances.</returns>
    public static async IAsyncEnumerable<T> QueryStreamAsync<T>(
        this IDbConnection connection, 
        ISqlQuery query, 
        IDbTransaction? transaction = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var compiler = GetCompiler(connection);
        var result = query.Build(compiler);
        
        using var metrics = new QueryMetrics("db.query_stream", result.Sql);
        var dynamicParams = new DynamicParameters();
        
        foreach (var param in result.Parameters)
        {
            dynamicParams.Add(param.Key, param.Value);
            metrics.AddParameter(param.Key, param.Value);
        }
        
        IAsyncEnumerator<T> enumerator;
        try
        {
            if (connection is not System.Data.Common.DbConnection dbConnection)
            {
                throw new System.NotSupportedException("QueryStreamAsync requires a connection that inherits from System.Data.Common.DbConnection.");
            }
            enumerator = dbConnection.QueryUnbufferedAsync<T>(result.Sql, dynamicParams, transaction as System.Data.Common.DbTransaction).GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex)
        {
            metrics.SetError(ex);
            throw;
        }

        await using (enumerator.ConfigureAwait(false))
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (Exception ex)
                {
                    metrics.SetError(ex);
                    throw;
                }

                if (!hasNext) break;

                yield return enumerator.Current;
            }
        }
    }

    
    /// <summary>
    /// Asynchronously executes a non-query SQL command and returns the number of rows affected.
    /// </summary>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the number of rows affected.
    /// </returns>
    public static Task<int> ExecuteAsync(
        this IDbConnection connection, 
        ISqlQuery query, 
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return RunDapperAsync(connection, query, "db.execute", transaction, cancellationToken,
            cmdDef => connection.ExecuteAsync(cmdDef));
    }

    // --- SYNC METHODS ---
    /// <summary>
    /// Synchronously executes a query and maps the results to strongly typed objects.
    /// </summary>
    /// <typeparam name="T">The type to map each result row to.</typeparam>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <returns>A collection of <typeparamref name="T"/> instances.</returns>
    public static IEnumerable<T> Query<T>(
        this IDbConnection connection, 
        ISqlQuery query, 
        IDbTransaction? transaction = null)
    {
        return RunDapper(connection, query, "db.query.sync", 
            (sql, dynamicParams) => connection.Query<T>(sql, dynamicParams, transaction));
    }

    /// <summary>
    /// Synchronously executes a non-query SQL command and returns the number of rows affected.
    /// </summary>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <returns>The number of rows affected.</returns>
    public static int Execute(
        this IDbConnection connection, 
        ISqlQuery query, 
        IDbTransaction? transaction = null)
    {
        return RunDapper(connection, query, "db.execute.sync", 
            (sql, dynamicParams) => connection.Execute(sql, dynamicParams, transaction));
    }

    // --- BULK HELPERS ---
    private static readonly System.Collections.Concurrent.ConcurrentBag<IBulkStrategy> _bulkStrategies = new();

    /// <summary>
    /// Registers a native bulk strategy for a specific database provider.
    /// </summary>
    /// <remarks>
    /// Registered strategies are evaluated in registration order when <see cref="BulkInsertAsync{T}"/> is called.
    /// The first strategy whose <see cref="IBulkStrategy.CanHandle"/> returns <see langword="true"/> is used.
    /// </remarks>
    /// <param name="strategy">The bulk strategy implementation to register (e.g., a <c>SqlBulkCopy</c> or <c>NpgsqlBinaryImporter</c> wrapper).</param>
    public static void RegisterBulkStrategy(IBulkStrategy strategy)
    {
        _bulkStrategies.Add(strategy);
    }

    /// <summary>
    /// Bulk inserts a collection of entities using the most appropriate registered native strategy, falling back to a parameterized batch insert.
    /// </summary>
    /// <typeparam name="T">The entity type to insert. Must have a default constructor and be a reference type.</typeparam>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the total number of rows inserted.
    /// </returns>
    public static Task<int> BulkInsertAsync<T>(this IDbConnection connection, IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where T : class, new()
    {
        // Stryker disable once NullCoalescing
        var list = entities as ICollection<T> ?? entities.ToList();
        if (list.Count == 0)
        {
            return Task.FromResult(0);
        }

        foreach (var strategy in _bulkStrategies)
        {
            if (strategy.CanHandle(connection))
            {
                return strategy.BulkInsertAsync(connection, list, transaction, cancellationToken);
            }
        }

        // Fallback to naive implementation
        var query = new InsertQuery<T>().Bulk(list);
        return connection.ExecuteAsync(query, transaction, cancellationToken);
    }
    
    /// <summary>
    /// Bulk updates a collection of entities using the specified template query.
    /// </summary>
    /// <typeparam name="T">The entity type to update.</typeparam>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="query">The UPDATE query template used as the SQL command text.</param>
    /// <param name="entities">The collection of entities whose values parameterize the update.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the total number of rows affected.
    /// </returns>
    public static Task<int> BulkUpdateAsync<T>(this IDbConnection connection, ISqlQuery query, IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return RunBulkAsync(connection, query, "db.bulk_update", entities, transaction, cancellationToken);
    }
    
    /// <summary>
    /// Bulk deletes a collection of entities using the specified template query.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="query">The DELETE query template used as the SQL command text.</param>
    /// <param name="entities">The collection of entities parameterizing the delete conditions.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the total number of rows affected.
    /// </returns>
    public static Task<int> BulkDeleteAsync<T>(this IDbConnection connection, ISqlQuery query, IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return RunBulkAsync(connection, query, "db.bulk_delete", entities, transaction, cancellationToken);
    }
    
    /// <summary>
    /// Asynchronously executes a query using <see cref="CommandBehavior.SequentialAccess"/> and maps rows using a custom reader delegate.
    /// </summary>
    /// <remarks>
    /// <see cref="CommandBehavior.SequentialAccess"/> reduces memory pressure when reading rows with large binary or text columns,
    /// as columns must be read in ordinal order.
    /// </remarks>
    /// <typeparam name="T">The type produced by the <paramref name="mapper"/> delegate.</typeparam>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="mapper">A delegate that reads a single row from the <see cref="IDataReader"/> and produces a <typeparamref name="T"/> instance.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the collection of mapped instances.
    /// </returns>
    public static Task<IEnumerable<T>> QuerySequentialAsync<T>(this IDbConnection connection, ISqlQuery query, System.Func<IDataReader, T> mapper, IDbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        return RunCommandAsync(connection, query, "db.query_sequential", transaction, async cmd =>
        {
            var list = new List<T>();
            if (cmd is System.Data.Common.DbCommand dbCmd)
            {
                using var reader = await dbCmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    list.Add(mapper(reader));
                }
            }
            else
            {
                using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
                while (reader.Read())
                {
                    list.Add(mapper(reader));
                }
            }
            return list.AsEnumerable();
        }, cancellationToken);
    }

    /// <summary>
    /// Asynchronously executes a query without reflection-based mapping, delegating row hydration to the provided delegate.
    /// </summary>
    /// <remarks>
    /// Suitable for AOT (Ahead-of-Time) compiled environments or scenarios where reflection is undesirable.
    /// Use alongside <c>SqlEntityGenerator</c> source-generated mappers for maximum performance.
    /// </remarks>
    /// <typeparam name="T">The type produced by the <paramref name="mapper"/> delegate.</typeparam>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="mapper">A reflection-free delegate that reads a single row and produces a <typeparamref name="T"/> instance.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the collection of mapped instances.
    /// </returns>
    public static Task<IEnumerable<T>> QueryAotAsync<T>(
        this IDbConnection connection, 
        ISqlQuery query, 
        System.Func<IDataReader, T> mapper, 
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return RunCommandAsync(connection, query, "db.query_aot", transaction, async cmd =>
        {
            var list = new List<T>();
            if (cmd is System.Data.Common.DbCommand dbCmd)
            {
                using var reader = await dbCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    list.Add(mapper(reader));
                }
            }
            else
            {
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(mapper(reader));
                }
            }
            return list.AsEnumerable();
        }, cancellationToken);
    }

    /// <summary>
    /// Asynchronously executes a query without reflection-based mapping and returns the first result, or <see langword="default"/> if the result set is empty.
    /// </summary>
    /// <typeparam name="T">The type produced by the <paramref name="mapper"/> delegate.</typeparam>
    /// <param name="connection">The database connection to execute against.</param>
    /// <param name="query">The SQL query to compile and execute.</param>
    /// <param name="mapper">A reflection-free delegate that reads a single row and produces a <typeparamref name="T"/> instance.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains the first mapped instance,
    /// or <see langword="default"/> if the result set is empty.
    /// </returns>
    public static Task<T?> QueryFirstOrDefaultAotAsync<T>(
        this IDbConnection connection, 
        ISqlQuery query, 
        System.Func<IDataReader, T> mapper, 
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        return RunCommandAsync(connection, query, "db.query_first_aot", transaction, async cmd =>
        {
            if (cmd is System.Data.Common.DbCommand dbCmd)
            {
                using var reader = await dbCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return mapper(reader);
                }
            }
            else
            {
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return mapper(reader);
                }
            }
            return default;
        }, cancellationToken);
    }
}
