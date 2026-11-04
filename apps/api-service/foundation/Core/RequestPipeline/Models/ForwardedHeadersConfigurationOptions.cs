namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record ForwardedHeadersConfigurationOptions
{
    /// <summary>
    /// Empty means only loopback is trusted (local development). In production, list the reverse proxies or load balancers
    /// in front of the app, or X-Forwarded-For/Proto/Host are ignored (wrong client IP and scheme).
    /// </summary>
    public string[] KnownProxies { get; init; } = [];
    public string[] KnownNetworks { get; init; } = [];
}
