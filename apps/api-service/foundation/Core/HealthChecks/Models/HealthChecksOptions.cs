using Aurore.Foundation.Core.HealthChecks.Constants;

namespace Aurore.Foundation.Core.HealthChecks.Models;

public sealed record HealthChecksOptions
{
    /// <summary>
    /// How long readiness/status responses are cached, so frequent probes don't hammer dependencies.
    /// </summary>
    public int CacheSeconds { get; init; } = HealthCheckProperties.Configuration.DefaultCacheSeconds;
}
