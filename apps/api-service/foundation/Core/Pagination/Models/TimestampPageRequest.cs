using System;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>A page request that can also filter by the creation and update times. See <see cref="ITimestampFilter"/>.</summary>
public class TimestampPageRequest : PageRequest, ITimestampFilter
{
    /// <inheritdoc />
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedTo { get; init; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedFrom { get; init; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedTo { get; init; }
}
