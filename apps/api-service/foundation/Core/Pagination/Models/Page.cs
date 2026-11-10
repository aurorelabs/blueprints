using System;
using System.Collections.Generic;
using System.Linq;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>One numbered page of a list, with the totals a client needs to build its pager.</summary>
/// <typeparam name="T">The type of item in the page.</typeparam>
public sealed record Page<T>
{
    /// <summary>The items of this page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>The 1-based number of this page.</summary>
    public required int Number { get; init; }

    /// <summary>The page size that was used.</summary>
    public required int PageSize { get; init; }

    /// <summary>How many items there are across all pages.</summary>
    public required int TotalItems { get; init; }

    /// <summary>How many pages there are. Zero when there are no items.</summary>
    public required int TotalPages { get; init; }

    /// <summary>Builds the page from the items already cut for it and the total count.</summary>
    public static Page<T> Create(IReadOnlyList<T> items, int number, int pageSize, int totalItems)
    {
        return new Page<T>
        {
            Items = items,
            Number = number,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
        };
    }

    /// <summary>The same page with each item transformed, for example from an entity to a response model.</summary>
    public Page<TResult> Map<TResult>(Func<T, TResult> transform)
    {
        return new Page<TResult>
        {
            Items = [.. Items.Select(transform)],
            Number = Number,
            PageSize = PageSize,
            TotalItems = TotalItems,
            TotalPages = TotalPages,
        };
    }
}
