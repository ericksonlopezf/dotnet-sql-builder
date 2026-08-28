// Copyright © Erickson Lopez. MIT License.
using System.Linq;

namespace EricksonLopez.SqlBuilder.MariaDb;

/// <summary>
/// Provides extension methods for MariaDB-specific SQL features.
/// </summary>
/// <remarks>
/// MariaDB uses the same backtick identifier quoting and SQL syntax as MySQL,
/// so these extensions mirror those found in <c>MySqlExtensions</c> but are
/// namespaced to <c>MariaDb</c> for clarity and discoverability.
/// </remarks>
public static class MariaDbExtensions
{
    // ─── JSON Functions ───────────────────────────────────────────────────────

    /// <summary>
    /// Adds a raw WHERE clause using MariaDB <c>JSON_EXTRACT</c> function.
    /// Generates: <c>WHERE JSON_EXTRACT(`column`, '$.path') = @p0</c>
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name containing the JSON document.</param>
    /// <param name="path">The JSON path expression (e.g. <c>"$.status"</c>).</param>
    /// <param name="value">The expected value to match.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the JSON_EXTRACT condition applied.</returns>
    /// <example>
    /// <code>
    /// query.WhereJsonExtract("metadata", "$.status", "active")
    /// // → WHERE JSON_EXTRACT(`metadata`, '$.status') = @p0
    /// </code>
    /// </example>
    public static SelectQuery<T> WhereJsonExtract<T>(
        this SelectQuery<T> query, string column, string path, object value) where T : class, new()
    {
        return query.Where(System.Runtime.CompilerServices.FormattableStringFactory.Create(
            $"JSON_EXTRACT(`{column}`, '{path}') = {{0}}", value));
    }

    /// <summary>
    /// Adds a SELECT clause using <c>JSON_ARRAYAGG</c> to aggregate column values into a JSON array.
    /// Generates: <c>JSON_ARRAYAGG(`column`) AS `alias`</c>
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column to aggregate into a JSON array.</param>
    /// <param name="alias">The alias name for the aggregated column.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the JSON_ARRAYAGG selection applied.</returns>
    public static SelectQuery<T> SelectJsonArrayAgg<T>(
        this SelectQuery<T> query, string column, string alias) where T : class, new()
    {
        return query.RawSelect(System.Runtime.CompilerServices.FormattableStringFactory.Create(
            $"JSON_ARRAYAGG(`{column}`) AS `{alias}`"));
    }

    /// <summary>
    /// Adds a SELECT clause using <c>JSON_OBJECTAGG</c> to aggregate key-value pairs into a JSON object.
    /// Generates: <c>JSON_OBJECTAGG(`keyColumn`, `valueColumn`) AS `alias`</c>
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
        return query.RawSelect(System.Runtime.CompilerServices.FormattableStringFactory.Create(
            $"JSON_OBJECTAGG(`{keyColumn}`, `{valueColumn}`) AS `{alias}`"));
    }

    // ─── Full-Text Search ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds a <c>WHERE MATCH AGAINST</c> full-text search clause.
    /// Generates: <c>WHERE MATCH(`col1`, `col2`) AGAINST (@p0 IN BOOLEAN MODE)</c>
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="searchTerm">The full-text search query string.</param>
    /// <param name="columns">The columns included in the full-text search index.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the full-text search condition applied.</returns>
    /// <remarks>
    /// The full-text index must exist on the specified columns.
    /// Example DDL: <c>CREATE FULLTEXT INDEX ft_products ON products(name, description);</c>
    /// </remarks>
    public static SelectQuery<T> WhereFullText<T>(
        this SelectQuery<T> query, string searchTerm, params string[] columns) where T : class, new()
    {
        var cols = string.Join(", ", columns.Select(c => $"`{c}`"));
        return query.Where(System.Runtime.CompilerServices.FormattableStringFactory.Create(
            $"MATCH({cols}) AGAINST ({{0}} IN BOOLEAN MODE)", searchTerm));
    }

    // ─── ON DUPLICATE KEY UPDATE helpers ─────────────────────────────────────

    /// <summary>
    /// Generates the raw <c>ON DUPLICATE KEY UPDATE</c> assignment string for a set of columns.
    /// </summary>
    /// <param name="columns">The columns to update on conflict.</param>
    /// <returns>The formatted SQL assignment clause.</returns>
    /// <example>
    /// <code>
    /// MariaDbExtensions.BuildOnDuplicateKeyUpdate("name", "email", "updated_at")
    /// // → "`name` = VALUES(`name`), `email` = VALUES(`email`), `updated_at` = VALUES(`updated_at`)"
    /// </code>
    /// </example>
    public static string BuildOnDuplicateKeyUpdate(params string[] columns)
    {
        return string.Join(", ", columns.Select(c => $"`{c}` = VALUES(`{c}`)"));
    }

    // ─── Pagination ───────────────────────────────────────────────────────────

    /// <summary>
    /// Appends <c>LIMIT / OFFSET</c> pagination for a given 1-based page number and page size.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SELECT query to paginate.</param>
    /// <param name="pageNumber">The 1-based page number (values ≤ 0 default to page 1).</param>
    /// <param name="pageSize">The number of rows per page (values ≤ 0 default to 10).</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the LIMIT and OFFSET applied.</returns>
    public static SelectQuery<T> Page<T>(this SelectQuery<T> query, int pageNumber, int pageSize)
        where T : class, new()
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
