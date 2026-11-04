using System.Collections.Generic;
using Aurore.Foundation.Core.HealthChecks.Constants;

namespace Aurore.Foundation.Core.HttpDependencies.Models;

public sealed record HttpDependencyHealthCheckOptions
{
    public string ReadinessEndpoint { get; init; } = HealthCheckProperties.Endpoints.Readiness;
    public int TimeoutSeconds { get; init; } = HealthCheckProperties.Configuration.DefaultTimeoutSeconds;

    /// <summary>Critical dependencies make this service Unhealthy (503) when they fail; others only Degraded (200).</summary>
    public bool IsCritical { get; init; }

    public List<string> Tags { get; init; } = [];
}
