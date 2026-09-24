// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Provides extension methods for applying dynamic sorting to queries using string-based column names.
/// </summary>
public static class DynamicSortingExtensions
{
    /// <summary>
    /// Appends an ORDER BY clause to the query using a string-based column name or property name.
    /// Safely resolves the property to its database column name using the entity's metadata cache.
    /// </summary>
    /// <typeparam name="T">The type of the entity to query.</typeparam>
    /// <param name="query">The query to apply the sorting to.</param>
    /// <param name="sortBy">The name of the property or column to sort by. Can include an alias prefix (e.g., "u.Name").</param>
    /// <param name="descending">If <see langword="true"/>, sorts in descending order; otherwise, ascending.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the sorting rule applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="sortBy"/> contains invalid characters and cannot be safely resolved</exception>
    public static SelectQuery<T> OrderByDynamic<T>(this SelectQuery<T> query, string sortBy, bool descending = false) where T : class, new()
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return query;
        }

        // Support aliases (e.g., "u.Name")
        var parts = sortBy.Split('.');
        if (parts.Length > 2)
        {
            throw new ArgumentException("Sort expression contains too many qualifiers.", nameof(sortBy));
        }

        string prefix = "";
        string propertyName;
        if (parts.Length == 2)
        {
            var alias = parts[0];
            if (!System.Text.RegularExpressions.Regex.IsMatch(alias, @"^[a-zA-Z0-9_]+$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                throw new ArgumentException("Invalid table alias in sort expression.", nameof(sortBy));
            }
            prefix = alias + ".";
            propertyName = parts[1];
        }
        else
        {
            propertyName = parts[0];
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(propertyName, @"^[a-zA-Z0-9_]+$", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2)))
        {
            throw new ArgumentException("Invalid sort column name", nameof(sortBy));
        }

        string columnName;
        if (SqlEntityCache<T>.PropertyMap.TryGetValue(propertyName, out var mapped))
        {
            columnName = mapped;
        }
        else
        {
            columnName = SqlNamingHelper.ToSnakeCase(propertyName);
        }
        return query.AddNode(new RawOrderByNode(prefix + columnName, descending));
    }
}


