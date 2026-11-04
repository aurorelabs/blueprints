namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record RateLimitPolicyOptions
{
    public int PermitLimit { get; init; } = 100;
    public int WindowSeconds { get; init; } = 60;
}
