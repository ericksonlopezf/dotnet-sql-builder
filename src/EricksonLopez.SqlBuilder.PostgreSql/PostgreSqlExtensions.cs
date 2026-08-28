// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Provides PostgreSQL-specific SQL extensions for building queries.
/// </summary>
public static class PostgreSqlExtensions
{
    /// <summary>
    /// Adds a DISTINCT ON clause to the query.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="columns">The columns to evaluate for distinctness.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the DISTINCT ON clause applied.</returns>
    public static SelectQuery<T> DistinctOn<T>(this SelectQuery<T> query, params string[] columns) where T : class, new()
    {
        return query.AddNode(new DistinctOnNode(columns));
    }
    
    /// <summary>
    /// Adds an ILIKE condition for case-insensitive pattern matching.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to compare.</param>
    /// <param name="pattern">The pattern to match against.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the ILIKE condition applied.</returns>
    public static SelectQuery<T> WhereILike<T>(this SelectQuery<T> query, string column, string pattern) where T : class, new()
    {
        return query.AddNode(new RawWhereNode($"{column} ILIKE {{0}}", new object[] { pattern }));
    }
    
    /// <summary>
    /// Adds a JSONB @&gt; condition to check if the column contains the specified JSON payload.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to check.</param>
    /// <param name="json">The JSON payload string.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the JSONB contains condition applied.</returns>
    public static SelectQuery<T> WhereJsonbContains<T>(this SelectQuery<T> query, string column, string json) where T : class, new()
    {
        return query.AddNode(new RawWhereNode($"{column} @> {{0}}::jsonb", new object[] { json }));
    }
    
    /// <summary>
    /// Adds a JSONB ? condition to check if a specific key exists at the top level.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to check.</param>
    /// <param name="key">The JSON key name.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the JSONB exists condition applied.</returns>
    public static SelectQuery<T> WhereJsonbExists<T>(this SelectQuery<T> query, string column, string key) where T : class, new()
    {
        return query.AddNode(new RawWhereNode($"{column} ? {{0}}", new object[] { key }));
    }
    
    /// <summary>
    /// Adds an = ANY() condition against an array.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to compare.</param>
    /// <param name="array">The array parameter value.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the ANY condition applied.</returns>
    public static SelectQuery<T> WhereAny<T>(this SelectQuery<T> query, string column, object array) where T : class, new()
    {
        return query.AddNode(new RawWhereNode($"{column} = ANY({{0}})", new object[] { array }));
    }
    
    /// <summary>
    /// Adds an = ALL() condition against an array.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to compare.</param>
    /// <param name="array">The array parameter value.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the ALL condition applied.</returns>
    public static SelectQuery<T> WhereAll<T>(this SelectQuery<T> query, string column, object array) where T : class, new()
    {
        return query.AddNode(new RawWhereNode($"{column} = ALL({{0}})", new object[] { array }));
    }

    /// <summary>
    /// Adds an array @&gt; condition to check if an array column contains elements.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to check.</param>
    /// <param name="array">The array elements parameter.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the array contains condition applied.</returns>
    public static SelectQuery<T> WhereArrayContains<T>(this SelectQuery<T> query, string column, object array) where T : class, new()
    {
        return query.AddNode(new RawWhereNode($"{column} @> {{0}}", new object[] { array }));
    }

    /// <summary>
    /// Adds an array &amp;&amp; condition to check for overlap between two arrays.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to check.</param>
    /// <param name="array">The array parameter value.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the array overlaps condition applied.</returns>
    public static SelectQuery<T> WhereArrayOverlaps<T>(this SelectQuery<T> query, string column, object array) where T : class, new()
    {
        return query.AddNode(new RawWhereNode($"{column} && {{0}}", new object[] { array }));
    }
    
    /// <summary>
    /// Adds a JSONPATH @@ condition to check if a jsonpath expression matches.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to check.</param>
    /// <param name="path">The JSONPATH expression string.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the JSONPATH condition applied.</returns>
    public static SelectQuery<T> WhereJsonPath<T>(this SelectQuery<T> query, string column, string path) where T : class, new()
    {
        return query.AddNode(new RawWhereNode($"{column} @@ {{0}}::jsonpath", new object[] { path }));
    }
    
    /// <summary>
    /// Adds a LATERAL join to a subquery.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="subquery">The subquery to join laterally.</param>
    /// <param name="alias">The alias name for the lateral join.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the LATERAL join applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="subquery"/> is not an AST query</exception>
    public static SelectQuery<T> JoinLateral<T>(this SelectQuery<T> query, ISqlQuery subquery, string alias) where T : class, new()
    {
        if (subquery is IAstQuery ast)
        {
            return query.AddNode(new SubqueryJoinNode(JoinType.Cross, ast, alias, IsLateral: true));
        }
        throw new System.ArgumentException("Subquery must be an AST query.");
    }
    
    /// <summary>
    /// Adds a FROM UNNEST() clause for one or more array parameters.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="alias">The alias name for the unnested table.</param>
    /// <param name="arrays">The array parameters to unnest.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the UNNEST clause applied.</returns>
    public static SelectQuery<T> FromUnnest<T>(this SelectQuery<T> query, string alias, params object[] arrays) where T : class, new()
    {
        return query.AddNode(new UnnestNode(arrays, alias));
    }
    
    /// <summary>
    /// Adds an aggregate function with a FILTER (WHERE ...) clause.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="aggregate">The aggregate function expression.</param>
    /// <param name="filter">The filter expression.</param>
    /// <param name="alias">The column alias name.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the filtered aggregate applied.</returns>
    public static SelectQuery<T> SelectFilter<T>(this SelectQuery<T> query, System.FormattableString aggregate, System.FormattableString filter, string alias) where T : class, new()
    {
        var aggArgs = aggregate.GetArguments();
        var filterArgs = filter.GetArguments();
        
        var combinedArgs = new object[aggArgs.Length + filterArgs.Length];
        Array.Copy(aggArgs, 0, combinedArgs, 0, aggArgs.Length);
        Array.Copy(filterArgs, 0, combinedArgs, aggArgs.Length, filterArgs.Length);
        
        // Re-index filter format placeholders (shift by aggArgs.Length)
        var filterFormat = System.Text.RegularExpressions.Regex.Replace(filter.Format, @"\{(\d+)\}", m => 
        {
            int index = int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            return "{" + (index + aggArgs.Length) + "}";
        }, System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2));
        
        return query.AddNode(new RawSelectNode($"{aggregate.Format} FILTER (WHERE {filterFormat}) AS {alias}", combinedArgs, false));
    }
    
    /// <summary>
    /// Adds a condition comparing against a strongly typed composite tuple.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to compare.</param>
    /// <param name="compositeTypeName">The PostgreSQL composite type name.</param>
    /// <param name="properties">The composite property values.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the composite condition applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="properties"/> is <see langword="null"/> or empty</exception>
    public static SelectQuery<T> WhereComposite<T>(this SelectQuery<T> query, string column, string compositeTypeName, params object[] properties) where T : class, new()
    {
        if (properties == null || properties.Length == 0)
        {
            throw new ArgumentException("Composite properties must not be empty.", nameof(properties));
        }

        var parameterPlaceholders = string.Join(", ", properties.Select((_, i) => "{" + i + "}"));
        string rawSql = $"{column} = ROW({parameterPlaceholders})::{compositeTypeName}";
        
        return query.AddNode(new RawWhereNode(rawSql, properties));
    }

    /// <summary>
    /// Adds a condition comparing against a custom PostgreSQL enum type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The SQL query AST builder.</param>
    /// <param name="column">The column name to compare.</param>
    /// <param name="enumTypeName">The PostgreSQL enum type name.</param>
    /// <param name="enumValue">The enum value.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the enum condition applied.</returns>
    public static SelectQuery<T> WherePgEnum<T>(this SelectQuery<T> query, string column, string enumTypeName, object enumValue) where T : class, new()
    {
        var val = enumValue is Enum e ? e.ToString() : enumValue.ToString();
        return query.AddNode(new RawWhereNode($"{column} = {{0}}::{enumTypeName}", new object?[] { val }));
    }
}





