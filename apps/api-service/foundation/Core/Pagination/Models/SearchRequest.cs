namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>
/// The base of every list request. Derive a request from it (or from PageRequest) and add the filters of the query.
/// Declare the derived request as a class with init properties, not a positional record, so that [AsParameters] binds the
/// inherited properties too.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// The orderings, repeated in the query: orderBy=name:asc&amp;orderBy=age:desc. The direction is optional and defaults to
    /// ascending. Which fields are allowed is decided by the SortMap of the query.
    /// </summary>
    public string[]? OrderBy { get; init; }
}
