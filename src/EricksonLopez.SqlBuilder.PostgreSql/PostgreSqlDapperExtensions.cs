// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Annotations;
using Npgsql;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Provides PostgreSQL-specific extension methods for Dapper connections.
/// </summary>
[RequiresDynamicCode("PostgreSQL Dapper extensions compile ISqlQuery using dynamic code generation. Use Sql.Raw() for strict NativeAOT paths.")]
[RequiresUnreferencedCode("PostgreSQL Dapper extensions access member metadata that may be trimmed. Use Sql.Raw() for strict NativeAOT paths.")]
public static class PostgreSqlDapperExtensions
{
    /// <summary>
    /// Executes a high-performance binary bulk copy using PostgreSQL COPY protocol.
    /// Requires NpgsqlConnection.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The PostgreSQL connection.</param>
    /// <param name="entities">The entities to copy.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Entity metadata column count does not match value count</exception>
    [ExcludeFromCodeCoverage]
    public static async Task BulkCopyAsync<T>(this NpgsqlConnection connection, IEnumerable<T> entities) where T : class, ISqlEntity, new()
    {
        var query = new CopyQuery<T>();
        var compiler = new PostgreSqlCompiler();
        var sqlResult = query.Build(compiler);
        
        using var writer = await connection.BeginBinaryImportAsync(sqlResult.Sql);
        
        var firstEntity = entities.FirstOrDefault();
        if (firstEntity != null)
        {
            var cols = firstEntity.GetColumnNames();
            var vals = firstEntity.GetValues();
            if (cols.Length != vals.Length)
            {
                throw new InvalidOperationException($"Entity metadata mismatch: GetColumnNames() returned {cols.Length} items, but GetValues() returned {vals.Length}. They must match.");
            }
        }
        
        foreach (var entity in entities)
        {
            var values = entity.GetValues();
            await writer.StartRowAsync();
            foreach (var val in values)
            {
                if (val == null)
                {
                    await writer.WriteNullAsync();
                }
                else
                {
                    await writer.WriteAsync(val);
                }
            }
        }
        await writer.CompleteAsync();
    }

    /// <summary>
    /// Executes a high-performance binary bulk copy using PostgreSQL COPY protocol via IDbConnection interface.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection (must be an <see cref="NpgsqlConnection"/>).</param>
    /// <param name="entities">The entities to copy.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="connection"/> is not an active <see cref="NpgsqlConnection"/></exception>
    public static Task BulkCopyAsync<T>(this System.Data.IDbConnection connection, IEnumerable<T> entities) where T : class, ISqlEntity, new()
    {
        // Stryker disable once Block : Justification: Underlying type verification to invoke strongly-typed overload
        if (connection is NpgsqlConnection npgConn)
        {
            return BulkCopyAsync(npgConn, entities);
        }
        throw new InvalidOperationException("BulkCopyAsync requires an active NpgsqlConnection.");
    }


    /// <summary>
    /// Executes a bulk INSERT using PostgreSQL's UNNEST function with array parameters.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <returns>The number of rows affected.</returns>
    /// <exception cref="InvalidOperationException">Entity metadata column count does not match value count</exception>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Npgsql UNNEST bulk insert constructs dynamic typed arrays.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("csharpsquid", "S2077:Formatting queries is susceptible to SQL injection", Justification = "Table and columns are validated identifiers, values are parameterized")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Table and columns are validated identifiers, values are parameterized")]
    public static async Task<int> BulkInsertUnnestAsync<T>(
        this IDbConnection connection, 
        IEnumerable<T> entities, 
        DbTransaction? transaction = null) where T : ISqlEntity, new()
    {
        var list = entities switch
        {
            IReadOnlyList<T> roList => roList,
            _ => entities.ToList()
        };
        if (list.Count == 0)
        {
            return 0;
        }

        var first = list[0];
        var table = first.GetTableName();
        var cols = first.GetColumnNames();
        var firstVals = first.GetValues();
        
        if (cols.Length != firstVals.Length)
        {
            throw new InvalidOperationException($"Entity metadata mismatch: GetColumnNames() returned {cols.Length} items, but GetValues() returned {firstVals.Length}. They must match.");
        }

        var arrays = new List<Array>(cols.Length);
        for (int i = 0; i < cols.Length; i++)
        {
            var sampleVal = first.GetValues()[i];
            var colType = sampleVal != null ? sampleVal.GetType() : typeof(object);
            var arr = Array.CreateInstance(colType, list.Count);
            for (int r = 0; r < list.Count; r++)
            {
                arr.SetValue(list[r].GetValues()[i], r);
            }
            arrays.Add(arr);
        }

        var colList = string.Join(", ", cols.Select(c => $"\"{c}\""));
        var paramNames = new string[cols.Length];
        for (int i = 0; i < cols.Length; i++)
        {
            paramNames[i] = $"@p{i}";
        }

        var sql = $"INSERT INTO \"{table}\" ({colList}) SELECT * FROM UNNEST({string.Join(", ", paramNames)})";

        using var cmd = connection.CreateCommand();
        if (transaction != null)
        {
            cmd.Transaction = transaction;
        }

        cmd.CommandText = sql;
        for (int i = 0; i < cols.Length; i++)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = $"p{i}";
            param.Value = arrays[i];
            cmd.Parameters.Add(param);
        }

        if (cmd is DbCommand dbCmd)
        {
            return await dbCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Executes a bulk INSERT using the PostgreSQL UNNEST strategy with explicit typed array parameters.
    /// Suitable for inserting large collections in a single round-trip when precise NpgsqlDbType
    /// control is required (e.g. <c>Uuid</c>, <c>TimestampTz</c>, <c>Jsonb</c>).
    /// </summary>
    /// <remarks>
    /// Build parameters with <see cref="BulkParameters"/>:
    /// <code>
    /// var p = BulkParameters.From(products)
    ///     .Add("Ids",    x => x.Id,    NpgsqlDbType.Uuid)
    ///     .Add("Names",  x => x.Name,  NpgsqlDbType.Text)
    ///     .Build();
    ///
    /// await connection.BulkInsertAsync(
    ///     "INSERT INTO products (id, name) SELECT * FROM UNNEST(@Ids, @Names)", p);
    /// </code>
    ///
    /// Performance vs. alternatives:
    /// <list type="bullet">
    ///   <item>Row-by-row INSERT: O(n) round-trips</item>
    ///   <item>Multi-value INSERT (...),(...): limited to ~65,535 parameters</item>
    ///   <item>UNNEST: 1 round-trip, no parameter limit</item>
    /// </list>
    /// </remarks>
    /// <param name="connection">Must be (or wrap) an <see cref="NpgsqlConnection"/>.</param>
    /// <param name="sql">The <c>INSERT … SELECT * FROM UNNEST(…)</c> statement.</param>
    /// <param name="parameters">Array parameters produced by <see cref="BulkParameters{T}.Build"/>.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds (default 30).</param>
    /// <returns>Number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="parameters"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is null or whitespace, or <paramref name="connection"/> is not an <see cref="NpgsqlConnection"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live PostgreSQL; covered by integration tests.")]
    public static async Task<int> BulkInsertAsync(
        this IDbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(parameters);

        if (parameters.Length == 0)
        {
            return 0;
        }

        var npgsqlConnection = connection as NpgsqlConnection
            ?? throw new ArgumentException(
                $"BulkInsertAsync requires an NpgsqlConnection. " +
                $"Received: {connection.GetType().Name}", nameof(connection));

        await using var command = npgsqlConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = commandTimeout ?? 30;

        if (transaction is not null)
        {
            command.Transaction = (NpgsqlTransaction)transaction;
        }

        command.Parameters.AddRange(parameters);

        if (npgsqlConnection.State != ConnectionState.Open)
        {
            await npgsqlConnection.OpenAsync().ConfigureAwait(false);
        }

        return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a bulk INSERT … ON CONFLICT DO UPDATE (upsert) using UNNEST.
    /// </summary>
    /// <remarks>
    /// Convenience alias of <see cref="BulkInsertAsync"/> — conflict handling is expressed in the SQL.
    /// </remarks>
    /// <param name="connection">Must be (or wrap) an <see cref="NpgsqlConnection"/>.</param>
    /// <param name="sql">The upsert statement including <c>ON CONFLICT … DO UPDATE</c>.</param>
    /// <param name="parameters">Array parameters produced by <see cref="BulkParameters{T}.Build"/>.</param>
    /// <param name="transaction">Optional transaction.</param>
    /// <param name="commandTimeout">Optional command timeout in seconds (default 30).</param>
    /// <returns>Number of rows affected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="parameters"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is null or whitespace, or <paramref name="connection"/> is not an <see cref="NpgsqlConnection"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live PostgreSQL; covered by integration tests.")]
    public static Task<int> BulkUpsertAsync(
        this IDbConnection connection,
        string sql,
        NpgsqlParameter[] parameters,
        IDbTransaction? transaction = null,
        int? commandTimeout = null)
        => connection.BulkInsertAsync(sql, parameters, transaction, commandTimeout);
}





