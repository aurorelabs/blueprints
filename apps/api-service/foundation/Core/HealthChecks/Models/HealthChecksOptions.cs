using Aurore.Foundation.Core.HealthChecks.Constants;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.HealthChecks.Models;

/// <summary>Settings of the health check endpoints.</summary>
[ExcludeFromCodeCoverage]
public sealed record HealthChecksOptions
{
    /// <summary>
    /// How long readiness/status responses are cached, so frequent probes don't hammer dependencies.
    /// </summary>
    public int CacheSeconds { get; init; } = HealthCheckProperties.Configuration.DefaultCacheSeconds;
}
