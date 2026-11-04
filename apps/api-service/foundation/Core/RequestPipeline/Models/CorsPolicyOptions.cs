namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record CorsPolicyOptions
{
    public string[] AllowedHeaders { get; init; } = [];
    public string[] AllowedMethods { get; init; } = [];
    public string[] AllowedOrigins { get; init; } = [];
    public string[] ExposedHeaders { get; init; } = [];

    /// <summary>
    /// Only needed when the browser must send cookies (cookie-based auth). Bearer tokens in the
    /// Authorization header do not require it. Cannot be combined with a wildcard ('*') origin.
    /// </summary>
    public bool AllowCredentials { get; init; }

    public int PreflightMaxAgeSeconds { get; init; }
}
