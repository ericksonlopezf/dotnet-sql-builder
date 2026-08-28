// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Fluent builder for inline window function expressions.
/// Use <see cref="Window"/> static factory methods to start a chain,
/// then call <see cref="As"/> to produce a <see cref="WindowFunctionNode"/>
/// that can be passed to <see cref="SelectQuery{T}.Select(WindowFunctionNode[])"/>.
/// </summary>
/// <example>
/// <code>
/// // RANK() OVER (PARTITION BY dept ORDER BY salary DESC) AS rank
/// var query = Sql.From&lt;Employee&gt;()
///     .Select(
///         Window.Rank()
///               .PartitionBy(e => e.Department)
///               .OrderByDescending(e => e.Salary)
///               .As("rank"));
///
/// // ROW_NUMBER() OVER (ORDER BY created_at) AS row_num
/// var query = Sql.From&lt;Order&gt;()
///     .Select(Window.RowNumber().OrderBy(o => o.CreatedAt).As("row_num"));
///
/// // LAG(amount, 1) OVER (PARTITION BY customer_id ORDER BY order_date) AS prev_amount
/// var query = Sql.From&lt;Order&gt;()
///     .Select(Window.Lag&lt;Order, decimal&gt;(o => o.Amount, offset: 1)
///                   .PartitionBy(o => o.CustomerId)
///                   .OrderBy(o => o.OrderDate)
///                   .As("prev_amount"));
/// </code>
/// </example>
public sealed class WindowBuilder<TEntity> where TEntity : class, new()
{
    private readonly string _functionName;
    private readonly string? _columnName;
    private readonly int? _offset;
    private readonly object? _defaultValue;
    private readonly List<string> _partitionByColumns = new();
    private readonly List<string> _orderByColumns = new();
    private readonly List<bool> _orderByDescending = new();
    private Expression? _filterExpression;
    private string? _filterRaw;
    private object?[]? _filterRawArgs;

    internal WindowBuilder(string functionName, string? columnName = null, int? offset = null, object? defaultValue = null)
    {
        _functionName = functionName;
        _columnName = columnName;
        _offset = offset;
        _defaultValue = defaultValue;
    }

    /// <summary>Adds a PARTITION BY column using a typed expression.</summary>
    /// <typeparam name="TKey">The type of the partition key.</typeparam>
    /// <param name="columnSelector">The expression selecting the partition column.</param>
    /// <returns>The current <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="columnSelector"/> is not a property access expression</exception>
    public WindowBuilder<TEntity> PartitionBy<TKey>(Expression<Func<TEntity, TKey>> columnSelector)
    {
        _partitionByColumns.Add(GetColumnName(columnSelector));
        return this;
    }

    /// <summary>Adds an ORDER BY column (ascending) using a typed expression.</summary>
    /// <typeparam name="TKey">The type of the order key.</typeparam>
    /// <param name="columnSelector">The expression selecting the order column.</param>
    /// <returns>The current <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="columnSelector"/> is not a property access expression</exception>
    public WindowBuilder<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> columnSelector)
    {
        _orderByColumns.Add(GetColumnName(columnSelector));
        _orderByDescending.Add(false);
        return this;
    }

    /// <summary>Adds an ORDER BY column (descending) using a typed expression.</summary>
    /// <typeparam name="TKey">The type of the order key.</typeparam>
    /// <param name="columnSelector">The expression selecting the order column.</param>
    /// <returns>The current <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="columnSelector"/> is not a property access expression</exception>
    public WindowBuilder<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> columnSelector)
    {
        _orderByColumns.Add(GetColumnName(columnSelector));
        _orderByDescending.Add(true);
        return this;
    }

    /// <summary>Adds a PARTITION BY column by raw column name (snake_case).</summary>
    /// <param name="columnName">The name of the partition column.</param>
    /// <returns>The current <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public WindowBuilder<TEntity> PartitionBy(string columnName)
    {
        _partitionByColumns.Add(columnName);
        return this;
    }

    /// <summary>Adds an ORDER BY column (ascending) by raw column name.</summary>
    /// <param name="columnName">The name of the order column.</param>
    /// <returns>The current <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public WindowBuilder<TEntity> OrderBy(string columnName)
    {
        _orderByColumns.Add(columnName);
        _orderByDescending.Add(false);
        return this;
    }

    /// <summary>Adds an ORDER BY column (descending) by raw column name.</summary>
    /// <param name="columnName">The name of the order column.</param>
    /// <returns>The current <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public WindowBuilder<TEntity> OrderByDescending(string columnName)
    {
        _orderByColumns.Add(columnName);
        _orderByDescending.Add(true);
        return this;
    }

    /// <summary>Adds a FILTER (WHERE ...) clause using a typed boolean LINQ predicate.</summary>
    /// <param name="filterPredicate">The predicate expression defining the filter condition.</param>
    /// <returns>The current <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public WindowBuilder<TEntity> Filter(Expression<Func<TEntity, bool>> filterPredicate)
    {
        _filterExpression = filterPredicate;
        return this;
    }

    /// <summary>Adds a FILTER (WHERE ...) clause using an interpolated string.</summary>
    /// <param name="rawCondition">The formattable string containing the filter expression.</param>
    /// <returns>The current <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public WindowBuilder<TEntity> Filter(FormattableString rawCondition)
    {
        _filterRaw = rawCondition.Format;
        _filterRawArgs = rawCondition.GetArguments();
        return this;
    }

    /// <summary>Adds a FILTER (WHERE ...) clause using raw SQL and parameters.</summary>
    /// <param name="rawCondition">The raw SQL filter expression.</param>
    /// <param name="parameters">The parameters to bind to the filter.</param>
    /// <returns>The current <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public WindowBuilder<TEntity> Filter(string rawCondition, params object?[] parameters)
    {
        _filterRaw = rawCondition;
        _filterRawArgs = parameters;
        return this;
    }

    /// <summary>
    /// Finalizes the window function expression with the given alias.
    /// </summary>
    /// <param name="alias">The AS alias name for this expression in the SELECT clause.</param>
    /// <returns>A <see cref="WindowFunctionNode"/> ready to pass into <c>.Select()</c>.</returns>
    /// <exception cref="ArgumentException"><paramref name="alias"/> is null, empty, or whitespace</exception>
    public WindowFunctionNode As(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            throw new ArgumentException("Alias cannot be null or empty.", nameof(alias));

        return new WindowFunctionNode(
            _functionName,
            _columnName,
            _offset,
            _defaultValue,
            _partitionByColumns.ToArray(),
            _orderByColumns.ToArray(),
            _orderByDescending.ToArray(),
            alias,
            _filterExpression,
            _filterRaw,
            _filterRawArgs
        );
    }

    private static string GetColumnName<TKey>(Expression<Func<TEntity, TKey>> selector)
    {
        if (selector.Body is MemberExpression member)
            return SqlNamingHelper.ToSnakeCase(member.Member.Name);
        throw new ArgumentException("Expression must be a property access (e.g. x => x.Property)", nameof(selector));
    }
}
