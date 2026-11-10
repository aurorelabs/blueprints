using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.HttpDependencies.Models;

/// <summary>
/// An upstream HTTP service. The authentication sections only hold values; the app chooses which one to apply when it
/// registers the client. Keep secrets (<c>Value</c>, <c>ClientSecret</c>) in environment variables or a secret store.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record HttpDependencyOptions
{
    /// <summary>The base URL of the upstream service.</summary>
    public required string BaseUrl { get; init; }

    /// <summary>Headers sent with every request, e.g. Accept, User-Agent or API version headers.</summary>
    public Dictionary<string, string> RequestHeaders { get; init; } = [];

    /// <summary>Forwards the current Correlation-Id. Turn off for third-party APIs that shouldn't see internal ids.</summary>
    public bool PropagateCorrelationId { get; init; } = true;

    /// <summary>A static header sent with every request, when the upstream authenticates by key.</summary>
    public UpstreamApiKeyOptions? ApiKey { get; init; }

    /// <summary>OAuth2 client credentials, when the upstream authenticates by token.</summary>
    public ClientCredentialsOptions? ClientCredentials { get; init; }

    /// <summary>When set, the upstream service is part of the readiness checks.</summary>
    public HttpDependencyHealthCheckOptions? HealthCheck { get; init; }
}
