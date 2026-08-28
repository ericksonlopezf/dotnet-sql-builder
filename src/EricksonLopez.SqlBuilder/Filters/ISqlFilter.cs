// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Filters;

/// <summary>
/// Defines a contract for applying modular, reusable filtering logic to a <see cref="SelectQuery{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being queried.</typeparam>
public interface ISqlFilter<TEntity> where TEntity : class, new()
{
    /// <summary>
    /// Applies the filtering logic to the specified query.
    /// </summary>
    /// <param name="query">The query to modify.</param>
    /// <returns>A new <see cref="SelectQuery{TEntity}"/> instance containing the applied filters.</returns>
    SelectQuery<TEntity> Apply(SelectQuery<TEntity> query);
}


