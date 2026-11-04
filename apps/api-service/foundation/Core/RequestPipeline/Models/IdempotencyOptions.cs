namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record IdempotencyOptions
{
    public int CacheDurationMinutes { get; init; } = 1440;
}
