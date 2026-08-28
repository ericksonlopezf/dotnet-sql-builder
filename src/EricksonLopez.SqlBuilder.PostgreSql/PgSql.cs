// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Provides PostgreSQL-specific operators for building SQL expressions (e.g., ILIKE, ANY, ALL).
/// </summary>
public static class PgSql
{
    /// <summary>
    /// Defines an ILike operator for SQL expressions in PostgreSQL.
    /// </summary>
    /// <param name="column">The column name to compare.</param>
    /// <param name="pattern">The pattern to match against.</param>
    /// <returns><see langword="true"/> if the column matches the pattern; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool ILike(string column, string pattern) => throw new InvalidOperationException("PgSql.ILike is for SQL expression building only.");

    /// <summary>
    /// Defines an ANY operator for SQL expressions in PostgreSQL.
    /// </summary>
    /// <typeparam name="TItem">The type of the item to compare.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="collection">The collection of items.</param>
    /// <returns><see langword="true"/> if the value matches any item in the collection; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool Any<TItem>(TItem value, IEnumerable<TItem> collection) => throw new InvalidOperationException("PgSql.Any is for SQL expression building only.");

    /// <summary>
    /// Defines an ALL operator for SQL expressions in PostgreSQL.
    /// </summary>
    /// <typeparam name="TItem">The type of the item to compare.</typeparam>
    /// <param name="value">The value to check.</param>
    /// <param name="collection">The collection of items.</param>
    /// <returns><see langword="true"/> if the value matches all items in the collection; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool All<TItem>(TItem value, IEnumerable<TItem> collection) => throw new InvalidOperationException("PgSql.All is for SQL expression building only.");
}
