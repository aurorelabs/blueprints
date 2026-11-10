using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>Which reverse proxies are trusted to tell the app the real client address and scheme.</summary>
[ExcludeFromCodeCoverage]
public sealed record ForwardedHeadersConfigurationOptions
{
    /// <summary>
    /// Empty means only loopback is trusted (local development). In production, list the reverse proxies or load balancers
    /// in front of the app, or X-Forwarded-For/Proto/Host are ignored (wrong client IP and scheme).
    /// </summary>
    public string[] KnownProxies { get; init; } = [];
    /// <summary>Trusted proxy networks in CIDR notation, for example 172.18.0.0/16 for a Docker network.</summary>
    public string[] KnownNetworks { get; init; } = [];
}
