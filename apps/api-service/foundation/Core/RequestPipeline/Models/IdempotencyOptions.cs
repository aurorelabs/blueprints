using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>Settings of the idempotency filter.</summary>
[ExcludeFromCodeCoverage]
public sealed record IdempotencyOptions
{
    /// <summary>
    /// How long the first successful result is replayed for the same Idempotency-Key. Defaults to 24 hours.
    /// </summary>
    public int CacheDurationSeconds { get; init; } = 86_400;
}
