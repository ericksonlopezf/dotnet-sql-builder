// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Provides extension methods for implementing keyset (cursor-based) pagination.
/// </summary>
public static class CursorPaginationExtensions
{
    /// <summary>
    /// Applies cursor-based pagination (seek method) to the query using the specified column and reference value.
    /// </summary>
    /// <typeparam name="T">The type of the entity to query.</typeparam>
    /// <param name="query">The query to paginate.</param>
    /// <param name="column">The expression specifying the seek column (usually a unique sequential identifier or timestamp).</param>
    /// <param name="lastValue">The value of the seek column from the last retrieved row.</param>
    /// <param name="ascending">If <see langword="true"/>, retrieves rows with values strictly greater than <paramref name="lastValue"/>; otherwise, strictly less than.</param>
    /// <param name="limit">The maximum number of rows to retrieve.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the seek condition, sorting, and row limit applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a valid property expression</exception>
    public static SelectQuery<T> Seek<T>(
        this SelectQuery<T> query,
        Expression<Func<T, object>> column,
        object lastValue,
        bool ascending = true,
        int limit = 10) where T : class, new()
    {
        var memberExp = column.Body as MemberExpression;
        var u = column.Body as UnaryExpression;
        if (memberExp == null && u != null)
        {
            memberExp = u.Operand as MemberExpression;
        }

        if (memberExp == null)
        {
            throw new ArgumentException("Must be a property expression");
        }

        var colName = SqlNamingHelper.ToSnakeCase(memberExp.Member.Name);
        
        var op = ascending ? ">" : "<";
        
        query = query.AddNode(new RawWhereNode($"{colName} {op} {{0}}", new object[] { lastValue }));
        query = ascending ? query.OrderBy(column) : query.OrderByDescending(column);
        return query.Limit(limit);
    }
}


