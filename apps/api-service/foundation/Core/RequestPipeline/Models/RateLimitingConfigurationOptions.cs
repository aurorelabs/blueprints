using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>Settings of the rate limiter.</summary>
[ExcludeFromCodeCoverage]
public sealed record RateLimitingConfigurationOptions
{
    /// <summary>
    /// Named policies; endpoints opt in with .RequireRateLimiting("&lt;name&gt;"). There is no global limiter.
    /// </summary>
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; init; } = [];
}
