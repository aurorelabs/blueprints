using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>
/// Defaults match Kestrel's own defaults; they are listed so they are visible and easy to change.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record KestrelLimitsOptions
{
    /// <summary>The most connections open at once. Null means no limit.</summary>
    public long? MaxConcurrentConnections { get; init; }
    /// <summary>The most headers one request may have.</summary>
    public int MaxRequestHeaderCount { get; init; } = 100;
    /// <summary>How long a client may take to send the request headers.</summary>
    public int RequestHeadersTimeoutSeconds { get; init; } = 30;
    /// <summary>How long an idle connection is kept open.</summary>
    public int KeepAliveTimeoutSeconds { get; init; } = 130;
    /// <summary>The largest request body accepted, in bytes. Null means no limit.</summary>
    public long? MaxRequestBodySizeBytes { get; init; } = 30_000_000;
}
