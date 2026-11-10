using System;
using System.Collections.Generic;
using System.Linq;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>One page of an infinite-scroll list. There is no total: the list is done when there is no next cursor.</summary>
/// <typeparam name="T">The type of item in the page.</typeparam>
public sealed record CursorPage<T>
{
    /// <summary>The items of this page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>The cursor to send for the next page, or null when this was the last one.</summary>
    public required string? NextCursor { get; init; }

    /// <summary>The page size that was used.</summary>
    public required int PageSize { get; init; }

    /// <summary>The same page with each item transformed, for example from an entity to a response model.</summary>
    public CursorPage<TResult> Map<TResult>(Func<T, TResult> transform)
    {
        return new CursorPage<TResult>
        {
            Items = [.. Items.Select(transform)],
            NextCursor = NextCursor,
            PageSize = PageSize,
        };
    }
}
