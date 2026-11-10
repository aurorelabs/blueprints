using System;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>A cursor request that can also filter by the creation and update times. See <see cref="ITimestampFilter"/>.</summary>
public class TimestampCursorRequest : CursorRequest, ITimestampFilter
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
