// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using EricksonLopez.Pagination.Abstractions;

namespace EricksonLopez.Pagination;

/// <summary>
/// Represents an immutable paginated list with an exact known total count.
/// </summary>
/// <typeparam name="T">The type of elements contained in the page.</typeparam>
public sealed class CountedPagedList<T> : PagedList<T>, ICountedPagedList<T>
{
    internal CountedPagedList(IReadOnlyList<T> items, long totalCount, int page, int pageSize)
        : base(items, totalCount, page, pageSize, hasNextPage: null, hasPreviousPage: null)
    {
    }

    long ICountedPagedList.TotalCount => ExactTotalCount;

    /// <summary>
    /// Gets the exact total number of items across all pages.
    /// </summary>
    public long ExactTotalCount => TotalCount!.Value;

    /// <summary>
    /// Projects each element of the counted paginated list into a new form using the specified transform function, preserving the total count.
    /// </summary>
    /// <typeparam name="TResult">The target element type.</typeparam>
    /// <param name="selector">The transform function.</param>
    /// <returns>A new <see cref="CountedPagedList{TResult}"/> containing the mapped elements.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/></exception>
    public new CountedPagedList<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        var mapped = new TResult[Count];
        for (int i = 0; i < Count; i++)
        {
            mapped[i] = selector(this[i]);
        }

        return new CountedPagedList<TResult>(mapped, ExactTotalCount, Page, PageSize);
    }
}
