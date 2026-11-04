namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>
/// Defaults match Kestrel's own defaults; they are listed so they are visible and easy to change.
/// </summary>
public sealed record KestrelLimitsOptions
{
    public long? MaxConcurrentConnections { get; init; }
    public int MaxRequestHeaderCount { get; init; } = 100;
    public int RequestHeadersTimeoutSeconds { get; init; } = 30;
    public int KeepAliveTimeoutSeconds { get; init; } = 130;
    public long? MaxRequestBodySizeBytes { get; init; } = 30_000_000;
}
