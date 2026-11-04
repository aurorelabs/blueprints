using System.Collections.Generic;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record RateLimitingConfigurationOptions
{
    /// <summary>
    /// Named policies; endpoints opt in with .RequireRateLimiting("&lt;name&gt;"). There is no global limiter.
    /// </summary>
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; init; } = [];
}
