using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>The page sizes every paged endpoint uses.</summary>
[ExcludeFromCodeCoverage]
public sealed record PaginationOptions
{
    /// <summary>The page size used when the request does not send one.</summary>
    public int DefaultPageSize { get; init; } = 20;

    /// <summary>The largest page size a request may ask for. A larger value is answered with a 400.</summary>
    public int MaxPageSize { get; init; } = 100;
}
