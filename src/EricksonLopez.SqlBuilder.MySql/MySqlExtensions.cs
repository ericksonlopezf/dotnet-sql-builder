// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;

namespace EricksonLopez.SqlBuilder.MySql;

/// <summary>
/// Provides extension methods for MySQL-specific SQL features.
/// </summary>
public static class MySqlExtensions
{
    // ─── JSON Functions ───────────────────────────────────────────────────────

    /// <summary>
    /// Adds a raw WHERE clause using MySQL JSON_EXTRACT function.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name containing the JSON document.</param>
    /// <param name="path">The JSON path expression (e.g. <c>"$.status"</c>).</param>
    /// <param name="value">The expected value to match.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the JSON_EXTRACT condition applied.</returns>
    public static SelectQuery<T> WhereJsonExtract<T>(
        this SelectQuery<T> query, string column, string path, object value) where T : class, new()
    {
        // Where(FormattableString) handles parameter binding automatically
        return query.Where(System.Runtime.CompilerServices.FormattableStringFactory.Create($"JSON_EXTRACT(`{column}`, '{path}') = {{0}}", value));
    }

    /// <summary>
    /// Adds a SELECT clause using JSON_ARRAYAGG to aggregate column values into a JSON array.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column to aggregate into a JSON array.</param>
    /// <param name="alias">The alias name for the aggregated column.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the JSON_ARRAYAGG selection applied.</returns>
    public static SelectQuery<T> SelectJsonArrayAgg<T>(
        this SelectQuery<T> query, string column, string alias) where T : class, new()
    {
        return query.RawSelect(System.Runtime.CompilerServices.FormattableStringFactory.Create($"JSON_ARRAYAGG(`{column}`) AS `{alias}`"));
    }

    /// <summary>
    /// Adds a SELECT clause using JSON_OBJECTAGG to aggregate key-value pairs into a JSON object.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="keyColumn">The column whose values will become JSON keys.</param>
    /// <param name="valueColumn">The column whose values will become JSON property values.</param>
    /// <param name="alias">The alias name for the aggregated column.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the JSON_OBJECTAGG selection applied.</returns>
    public static SelectQuery<T> SelectJsonObjectAgg<T>(
        this SelectQuery<T> query, string keyColumn, string valueColumn, string alias) where T : class, new()
    {
        return query.RawSelect(System.Runtime.CompilerServices.FormattableStringFactory.Create($"JSON_OBJECTAGG(`{keyColumn}`, `{valueColumn}`) AS `{alias}`"));
    }

    // ─── Full-Text Search ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds a WHERE MATCH AGAINST full-text search clause in boolean mode.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="searchTerm">The full-text search query string.</param>
    /// <param name="columns">The columns included in the full-text search index.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the full-text search condition applied.</returns>
    public static SelectQuery<T> WhereFullText<T>(
        this SelectQuery<T> query, string searchTerm, params string[] columns) where T : class, new()
    {
        var cols = string.Join(", ", columns.Select(c => $"`{c}`"));
        // Use RawWhere with the format string {0} pattern for parameter binding
        return query.Where(System.Runtime.CompilerServices.FormattableStringFactory.Create($"MATCH({cols}) AGAINST ({{0}} IN BOOLEAN MODE)", searchTerm));
    }

    // ─── ON DUPLICATE KEY UPDATE helpers ─────────────────────────────────────

    /// <summary>
    /// Generates the raw ON DUPLICATE KEY UPDATE assignment string for a set of columns.
    /// </summary>
    /// <param name="columns">The columns to update on conflict.</param>
    /// <returns>The formatted SQL assignment clause.</returns>
    public static string BuildOnDuplicateKeyUpdate(params string[] columns)
    {
        return string.Join(", ", columns.Select(c => $"`{c}` = VALUES(`{c}`)"));
    }

    // ─── Pagination ───────────────────────────────────────────────────────────

    /// <summary>
    /// Appends LIMIT / OFFSET pagination for a given 1-based page number and page size.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the LIMIT and OFFSET applied.</returns>
    public static SelectQuery<T> Page<T>(this SelectQuery<T> query, int pageNumber, int pageSize) where T : class, new()
    {
        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        return query.Limit(pageSize).Offset((pageNumber - 1) * pageSize);
    }
}


