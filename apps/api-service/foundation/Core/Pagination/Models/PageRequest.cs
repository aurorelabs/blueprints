namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>
/// A list request that asks for one numbered page. Both values are optional: a missing page is the first one and a
/// missing page size is the DefaultPageSize of the pagination settings.
/// </summary>
public class PageRequest : SearchRequest
{
    /// <summary>The 1-based number of the page.</summary>
    public int? Page { get; init; }

    /// <summary>How many items a page holds, up to the MaxPageSize of the pagination settings.</summary>
    public int? PageSize { get; init; }
}
