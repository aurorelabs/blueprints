using System;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>
/// Ranges over the creation and update times. Every range starts at its From (inclusive) and ends before its To
/// (exclusive), so two ranges that touch never count the same item twice.
/// </summary>
public interface ITimestampFilter
{
    /// <summary>Items created at or after this time.</summary>
    public DateTimeOffset? CreatedFrom { get; }

    /// <summary>Items created before this time.</summary>
    public DateTimeOffset? CreatedTo { get; }

    /// <summary>Items updated at or after this time.</summary>
    public DateTimeOffset? UpdatedFrom { get; }

    /// <summary>Items updated before this time.</summary>
    public DateTimeOffset? UpdatedTo { get; }
}
