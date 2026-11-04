namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record RequestPipelineOptions
{
    public ForwardedHeadersConfigurationOptions ForwardedHeaders { get; init; } = new();
    public IdempotencyOptions Idempotency { get; init; } = new();
    public KestrelLimitsOptions KestrelLimits { get; init; } = new();
    public RateLimitingConfigurationOptions RateLimiting { get; init; } = new();
    public ResponseCompressionConfigurationOptions ResponseCompression { get; init; } = new();
    public SecurityHeadersOptions SecurityHeaders { get; init; } = new();
    public RequestTimeoutsConfigurationOptions Timeouts { get; init; } = new();
}
