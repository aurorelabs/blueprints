using System.Collections.Generic;
using Aurore.Foundation.Core.HealthChecks.Constants;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.HttpDependencies.Models;

/// <summary>How the health of an upstream HTTP service is checked.</summary>
[ExcludeFromCodeCoverage]
public sealed record HttpDependencyHealthCheckOptions
{
    /// <summary>The path called on the upstream service, relative to its base URL.</summary>
    public string ReadinessEndpoint { get; init; } = HealthCheckProperties.Endpoints.Readiness;
    /// <summary>How long the call may take before the check fails.</summary>
    public int TimeoutSeconds { get; init; } = HealthCheckProperties.Configuration.DefaultTimeoutSeconds;

    /// <summary>Critical dependencies make this service Unhealthy (503) when they fail; others only Degraded (200).</summary>
    public bool Critical { get; init; }

    /// <summary>Extra tags added to the health check.</summary>
    public List<string> Tags { get; init; } = [];
}
