// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NpgsqlTypes;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Represents a builder for array parameters required for PostgreSQL UNNEST bulk operations.
/// </summary>
/// <remarks>
/// UNNEST allows inserting thousands of rows in a single query by passing arrays of values,
/// avoiding the N+1 problem and parameter count limits of multi-value INSERT statements.
///
/// Usage:
/// <code>
/// var parameters = BulkParameters.From(products)
///     .Add("Ids",        p => p.Id,        NpgsqlDbType.Uuid)
///     .Add("Names",      p => p.Name,      NpgsqlDbType.Text)
///     .Add("Prices",     p => p.Price,     NpgsqlDbType.Numeric)
///     .Add("CreatedAts", p => p.CreatedAt, NpgsqlDbType.TimestampTz)
///     .Build();
///
/// var sql = """
///     INSERT INTO products (id, name, price, created_at)
///     SELECT * FROM UNNEST(@Ids, @Names, @Prices, @CreatedAts)
///     """;
///
/// await connection.BulkInsertAsync(sql, parameters);
/// </code>
/// </remarks>
/// <typeparam name="T">The entity type being bulk-inserted.</typeparam>
public sealed class BulkParameters<T>
{
    private readonly List<T> _items;
    private readonly List<(string Name, Array Values, NpgsqlDbType DbType)> _columns = [];

    internal BulkParameters(IEnumerable<T> items)
    {
        _items = items.ToList();
    }

    /// <summary>
    /// Adds a column mapping: extracts a value from each item and stores it as a typed array.
    /// </summary>
    /// <typeparam name="TValue">The type of the extracted column values.</typeparam>
    /// <param name="parameterName">The UNNEST parameter name (used in the SQL query as @Name).</param>
    /// <param name="selector">Extracts the column value from each item.</param>
    /// <param name="dbType">The PostgreSQL type of the column.</param>
    /// <returns>The current <see cref="BulkParameters{T}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="parameterName"/> is null, empty, or whitespace</exception>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    public BulkParameters<T> Add<TValue>(
        string parameterName,
        Func<T, TValue> selector,
        NpgsqlDbType dbType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentNullException.ThrowIfNull(selector);

        var values = new TValue[_items.Count];
        for (int i = 0; i < _items.Count; i++)
        {
            values[i] = selector(_items[i]);
        }
        _columns.Add((parameterName, values, dbType));
        return this;
    }

    /// <summary>
    /// Builds the <see cref="NpgsqlParameter"/> collection ready to pass to
    /// <see cref="PostgreSqlDapperExtensions.BulkInsertAsync"/>.
    /// </summary>
    /// <returns>An array of configured <see cref="NpgsqlParameter"/> instances.</returns>
    /// <exception cref="InvalidOperationException">No columns have been added via <see cref="Add{TValue}"/></exception>
    public NpgsqlParameter[] Build()
    {
        if (_columns.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one column must be added via Add() before calling Build().");
        }

        return _columns
            .Select(col => new NpgsqlParameter(col.Name, col.DbType | NpgsqlDbType.Array)
            {
                Value = col.Values
            })
            .ToArray();
    }

    /// <summary>
    /// Gets the number of items that will be inserted.
    /// </summary>
    public int Count => _items.Count;
}
