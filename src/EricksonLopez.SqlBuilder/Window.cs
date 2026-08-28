// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Static factory for creating typed window function builders.
/// </summary>
/// <remarks>
/// Usage: <c>Window.Rank&lt;Employee&gt;().PartitionBy(e => e.Dept).OrderByDescending(e => e.Salary).As("rnk")</c>
/// </remarks>
public static class Window
{
    /// <summary>Creates a ROW_NUMBER() window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public static WindowBuilder<T> RowNumber<T>() where T : class, new()
        => new("ROW_NUMBER");

    /// <summary>Creates a RANK() window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public static WindowBuilder<T> Rank<T>() where T : class, new()
        => new("RANK");

    /// <summary>Creates a DENSE_RANK() window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public static WindowBuilder<T> DenseRank<T>() where T : class, new()
        => new("DENSE_RANK");

    /// <summary>Creates a NTILE(n) window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="buckets">The number of buckets to split rows into.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public static WindowBuilder<T> Ntile<T>(int buckets) where T : class, new()
        => new("NTILE", columnName: buckets.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Creates a LAG(column, offset) window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <param name="offset">The row offset count.</param>
    /// <param name="defaultValue">The optional fallback value when offset is out of bounds.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> Lag<T, TCol>(Expression<Func<T, TCol>> column, int offset = 1, object? defaultValue = null) where T : class, new()
    {
        var colName = GetColName(column);
        return new WindowBuilder<T>("LAG", colName, offset, defaultValue);
    }

    /// <summary>Creates a LEAD(column, offset) window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <param name="offset">The row offset count.</param>
    /// <param name="defaultValue">The optional fallback value when offset is out of bounds.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> Lead<T, TCol>(Expression<Func<T, TCol>> column, int offset = 1, object? defaultValue = null) where T : class, new()
    {
        var colName = GetColName(column);
        return new WindowBuilder<T>("LEAD", colName, offset, defaultValue);
    }

    /// <summary>Creates a SUM(column) OVER window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> Sum<T, TCol>(Expression<Func<T, TCol>> column) where T : class, new()
        => new(GetColName(column) is var c ? "SUM" : "SUM", c);

    /// <summary>Creates an AVG(column) OVER window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> Avg<T, TCol>(Expression<Func<T, TCol>> column) where T : class, new()
        => new("AVG", GetColName(column));

    /// <summary>Creates a COUNT(*) OVER window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public static WindowBuilder<T> Count<T>() where T : class, new()
        => new("COUNT");

    /// <summary>Creates a MIN(column) OVER window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> Min<T, TCol>(Expression<Func<T, TCol>> column) where T : class, new()
        => new("MIN", GetColName(column));

    /// <summary>Creates a MAX(column) OVER window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> Max<T, TCol>(Expression<Func<T, TCol>> column) where T : class, new()
        => new("MAX", GetColName(column));

    /// <summary>Creates a FIRST_VALUE(column) OVER window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> FirstValue<T, TCol>(Expression<Func<T, TCol>> column) where T : class, new()
        => new("FIRST_VALUE", GetColName(column));

    /// <summary>Creates a LAST_VALUE(column) OVER window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> LastValue<T, TCol>(Expression<Func<T, TCol>> column) where T : class, new()
        => new("LAST_VALUE", GetColName(column));

    /// <summary>Creates a CUME_DIST() window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public static WindowBuilder<T> CumeDist<T>() where T : class, new()
        => new("CUME_DIST");

    /// <summary>Creates a PERCENT_RANK() window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    public static WindowBuilder<T> PercentRank<T>() where T : class, new()
        => new("PERCENT_RANK");

    /// <summary>Creates a NTH_VALUE(column, n) window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <param name="n">The nth position index.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> NthValue<T, TCol>(Expression<Func<T, TCol>> column, int n) where T : class, new()
        => new("NTH_VALUE", $"{GetColName(column)}, {n.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

    /// <summary>Creates a STDDEV_SAMP(column) OVER window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> StdDev<T, TCol>(Expression<Func<T, TCol>> column) where T : class, new()
        => new("STDDEV_SAMP", GetColName(column));

    /// <summary>Creates a VARIANCE(column) OVER window function builder.</summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TCol">The type of the column.</typeparam>
    /// <param name="column">The expression selecting the column.</param>
    /// <returns>A new <see cref="WindowBuilder{TEntity}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is not a property access expression</exception>
    public static WindowBuilder<T> Variance<T, TCol>(Expression<Func<T, TCol>> column) where T : class, new()
        => new("VAR_SAMP", GetColName(column));

    private static string GetColName<T, TCol>(Expression<Func<T, TCol>> selector)
    {
        if (selector.Body is MemberExpression member)
            return SqlNamingHelper.ToSnakeCase(member.Member.Name);
        throw new ArgumentException("Expression must be a property access (e.g. x => x.Property)", nameof(selector));
    }
}
