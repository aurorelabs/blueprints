using Aurore.Foundation.Core.Pagination.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Aurore.Foundation.AspNetCore.Pagination.Extensions;

/// <summary>Documents the responses of paged endpoints.</summary>
public static class PaginationEndpointExtensions
{
    /// <summary>
    /// Declares the 200 response as a Page of T, in place of Produces of a collection. The 400 for invalid paging parameters is
    /// already declared for every endpoint.
    /// </summary>
    public static RouteHandlerBuilder ProducesPage<T>(this RouteHandlerBuilder builder)
    {
        return builder.Produces<Page<T>>();
    }

    /// <summary>Declares the 200 response as a CursorPage of T, for infinite-scroll lists.</summary>
    public static RouteHandlerBuilder ProducesCursorPage<T>(this RouteHandlerBuilder builder)
    {
        return builder.Produces<CursorPage<T>>();
    }
}
