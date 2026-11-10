using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>One CORS policy: which browser origins may call the service and how.</summary>
[ExcludeFromCodeCoverage]
public sealed record CorsPolicyOptions
{
    /// <summary>Request headers the browser may send. Empty uses the platform's defaults.</summary>
    public string[] AllowedHeaders { get; init; } = [];
    /// <summary>HTTP methods the browser may use. Empty uses the platform's defaults.</summary>
    public string[] AllowedMethods { get; init; } = [];
    /// <summary>The origins (scheme, host and port) allowed to call the service.</summary>
    public string[] AllowedOrigins { get; init; } = [];
    /// <summary>Response headers the browser may read. Empty uses the platform's defaults.</summary>
    public string[] ExposedHeaders { get; init; } = [];

    /// <summary>
    /// Only needed when the browser must send cookies (cookie-based auth). Bearer tokens in the
    /// Authorization header do not require it. Cannot be combined with a wildcard ('*') origin.
    /// </summary>
    public bool AllowCredentials { get; init; }

    /// <summary>
    /// How long browsers may cache the preflight answer (Access-Control-Max-Age).
    /// </summary>
    public int PreflightMaxAgeSeconds { get; init; }
}
