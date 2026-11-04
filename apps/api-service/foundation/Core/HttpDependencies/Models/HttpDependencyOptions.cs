using System.Collections.Generic;

namespace Aurore.Foundation.Core.HttpDependencies.Models;

/// <summary>
/// An upstream HTTP service. The authentication sections only hold values; the app chooses which one to apply when it
/// registers the client. Keep secrets (<c>Value</c>, <c>ClientSecret</c>) in environment variables or a secret store.
/// </summary>
public sealed record HttpDependencyOptions
{
    public required string BaseUrl { get; init; }

    /// <summary>Headers sent with every request, e.g. Accept, User-Agent or API version headers.</summary>
    public Dictionary<string, string> RequestHeaders { get; init; } = [];

    /// <summary>Forwards the current Correlation-Id. Turn off for third-party APIs that shouldn't see internal ids.</summary>
    public bool PropagateCorrelationId { get; init; } = true;

    public UpstreamApiKeyOptions? ApiKey { get; init; }

    public ClientCredentialsOptions? ClientCredentials { get; init; }

    public HttpDependencyHealthCheckOptions? HealthCheck { get; init; }
}
