// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Represents an immutable paginated list providing offset-based pagination metadata.
/// </summary>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public class PagedList<T> : IPagedList<T>
{
    private readonly IReadOnlyList<T> _items;
    private readonly bool? _hasPreviousPage;

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedList{T}"/> class.
    /// </summary>
    internal PagedList(
        IReadOnlyList<T> items,
        long? totalCount,
        int page,
        int pageSize,
        bool? hasNextPage = null,
        bool? hasPreviousPage = null)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be greater than or equal to 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be greater than or equal to 1.");
        if (totalCount.HasValue && totalCount.Value < 0) throw new ArgumentOutOfRangeException(nameof(totalCount), "TotalCount cannot be negative.");

        _items = items ?? Array.Empty<T>();
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = totalCount.HasValue ? (totalCount.Value + pageSize - 1L) / pageSize : null;

        if (totalCount.HasValue)
        {
            if (totalCount.Value == 0)
            {
                HasNextPage = false;
            }
            else
            {
                HasNextPage = Page < TotalPages;
            }
        }
        else if (hasNextPage.HasValue)
        {
            HasNextPage = hasNextPage.Value;
        }

        _hasPreviousPage = hasPreviousPage;
    }

    /// <inheritdoc />
    public long? TotalCount { get; }

    /// <summary>
    /// Gets the current page number (1-indexed).
    /// </summary>
    public int Page { get; }

    /// <inheritdoc />
    public int PageSize { get; }

    /// <inheritdoc />
    public long? TotalPages { get; }

    /// <inheritdoc />
    public bool HasPreviousPage 
    {
        get
        {
            if (_hasPreviousPage.HasValue) return _hasPreviousPage.Value;
            return Page > 1;
        }
    }

    /// <inheritdoc />
    public bool HasNextPage { get; }

    /// <inheritdoc />
    public int Count => _items.Count;

    /// <inheritdoc />
    public T this[int index] => _items[index];

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Creates a new <see cref="CountedPagedList{T}"/> with the specified items, pagination parameters, and known total count.
    /// </summary>
    /// <param name="items">The items of the current page.</param>
    /// <param name="parameters">The pagination parameters.</param>
    /// <param name="totalCount">The total count of matching records.</param>
    /// <returns>A new <see cref="CountedPagedList{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    [Pure]
    public static CountedPagedList<T> WithCount(
        IReadOnlyList<T> items,
        PaginationParameters parameters,
        long totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new CountedPagedList<T>(items, totalCount, parameters.Page, parameters.PageSize);
    }

    /// <summary>
    /// Creates a new <see cref="PagedList{T}"/> without an exact total count using forward-probe indicator metadata.
    /// </summary>
    /// <param name="items">The items of the current page.</param>
    /// <param name="parameters">The pagination parameters.</param>
    /// <param name="hasNextPage">Indicates whether a next page exists.</param>
    /// <param name="hasPreviousPage">Indicates whether a previous page exists.</param>
    /// <returns>A new <see cref="PagedList{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <see langword="null"/></exception>
    [Pure]
    public static PagedList<T> WithoutCount(
        IReadOnlyList<T> items,
        PaginationParameters parameters,
        bool hasNextPage,
        bool? hasPreviousPage = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new PagedList<T>(items, null, parameters.Page, parameters.PageSize, hasNextPage, hasPreviousPage);
    }

    /// <summary>
    /// Creates an empty <see cref="CountedPagedList{T}"/> with a total count of zero.
    /// </summary>
    /// <param name="parameters">The pagination parameters.</param>
    /// <returns>An empty <see cref="CountedPagedList{T}"/>.</returns>
    [Pure]
    public static CountedPagedList<T> Empty(PaginationParameters parameters)
    {
        return new CountedPagedList<T>(Array.Empty<T>(), 0, parameters.Page, parameters.PageSize);
    }

    /// <summary>
    /// Projects each element of the paginated list into a new form using the specified transform function.
    /// </summary>
    /// <typeparam name="TResult">The target element type.</typeparam>
    /// <param name="selector">The transform function.</param>
    /// <returns>A new <see cref="PagedList{TResult}"/> containing the mapped elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    public PagedList<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var mapped = new TResult[_items.Count];
        for (int i = 0; i < _items.Count; i++)
        {
            mapped[i] = selector(_items[i]);
        }

        return new PagedList<TResult>(mapped, TotalCount, Page, PageSize, HasNextPage, _hasPreviousPage);
    }
}
