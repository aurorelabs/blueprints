using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>Settings of everything a request passes through before it reaches an endpoint.</summary>
[ExcludeFromCodeCoverage]
public sealed record RequestPipelineOptions
{
    /// <summary>Named CORS policies. Empty means CORS is not enabled.</summary>
    public IReadOnlyDictionary<string, CorsPolicyOptions> CorsPolicies { get; init; } = new Dictionary<string, CorsPolicyOptions>();
    /// <summary>The trusted reverse proxies.</summary>
    public ForwardedHeadersConfigurationOptions ForwardedHeaders { get; init; } = new();
    /// <summary>Idempotency filter settings.</summary>
    public IdempotencyOptions Idempotency { get; init; } = new();
    /// <summary>Limits of the web server.</summary>
    public KestrelLimitsOptions KestrelLimits { get; init; } = new();
    /// <summary>Named output cache policies.</summary>
    public OutputCacheConfigurationOptions OutputCache { get; init; } = new();
    /// <summary>Encryption of personal data in logs and traces.</summary>
    public PrivacyOptions Privacy { get; init; } = new();
    /// <summary>Named rate limit policies.</summary>
    public RateLimitingConfigurationOptions RateLimiting { get; init; } = new();
    /// <summary>Response compression settings.</summary>
    public ResponseCompressionConfigurationOptions ResponseCompression { get; init; } = new();
    /// <summary>Security headers added to every response.</summary>
    public SecurityHeadersOptions SecurityHeaders { get; init; } = new();
    /// <summary>Request timeouts.</summary>
    public RequestTimeoutsConfigurationOptions Timeouts { get; init; } = new();
}
