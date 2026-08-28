// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NpgsqlTypes;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Provides factory methods for creating bulk PostgreSQL array parameters.
/// </summary>
public static class BulkParameters
{
    /// <summary>
    /// Begins building bulk parameters for the given collection.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="items">The collection of items to insert.</param>
    /// <returns>A new <see cref="BulkParameters{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    public static BulkParameters<T> From<T>(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new BulkParameters<T>(items);
    }
}
