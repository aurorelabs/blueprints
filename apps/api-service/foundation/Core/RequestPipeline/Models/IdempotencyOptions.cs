namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record IdempotencyOptions
{
    /// <summary>
    /// How long the first successful result is replayed for the same Idempotency-Key. Defaults to 24 hours.
    /// </summary>
    public int CacheDurationSeconds { get; init; } = 86_400;
}
