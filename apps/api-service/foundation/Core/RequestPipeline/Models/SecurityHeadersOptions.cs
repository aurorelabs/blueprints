using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>Settings of the security headers.</summary>
[ExcludeFromCodeCoverage]
public sealed record SecurityHeadersOptions
{
    /// <summary>
    /// Applied in production only; outside production it would block the Scalar UI.
    /// </summary>
    public string? ContentSecurityPolicy { get; init; } = "default-src 'none'; frame-ancestors 'none'";
}
