// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Filters;

/// <summary>
/// Provides extension methods for applying <see cref="ISqlFilter{TEntity}"/> instances to queries.
/// </summary>
public static class FilterExtensions
{
    /// <summary>
    /// Applies the specified filter to the query.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being queried.</typeparam>
    /// <param name="query">The query to modify.</param>
    /// <param name="filter">The filter to apply, or <see langword="null"/> to skip filtering.</param>
    /// <returns>A new <see cref="SelectQuery{TEntity}"/> instance containing the applied filter, or the original query if the filter is null.</returns>
    public static SelectQuery<TEntity> ApplyFilter<TEntity>(this SelectQuery<TEntity> query, ISqlFilter<TEntity>? filter) where TEntity : class, new()
    {
        if (filter == null)
        {
            return query;
        }

        return filter.Apply(query);
    }

    /// <summary>
    /// Applies a collection of filters sequentially to the query.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity being queried.</typeparam>
    /// <param name="query">The query to modify.</param>
    /// <param name="filters">An array of filters to apply. Null filters in the array are ignored.</param>
    /// <returns>A new <see cref="SelectQuery{TEntity}"/> instance containing all applied filters.</returns>
    public static SelectQuery<TEntity> ApplyFilters<TEntity>(this SelectQuery<TEntity> query, params ISqlFilter<TEntity>[] filters) where TEntity : class, new()
    {
        if (filters == null || filters.Length == 0)
        {
            return query;
        }

        foreach (var filter in filters)
        {
            if (filter != null)
            {
                query = filter.Apply(query);
            }
        }
        return query;
    }
}


