using System.Collections.Generic;
using Aurore.Foundation.Core;
using Aurore.Foundation.Telemetry;

namespace Aurore.Blueprints.ApiService.Core;

public sealed record AppSettings
{
    public required AppInformationOptions AppInformation { get; init; }
    public IReadOnlyDictionary<string, CorsPolicyOptions> CorsPolicies { get; init; } = new Dictionary<string, CorsPolicyOptions>();
    public required IdentityProviderOptions IdentityProvider { get; init; }
    public required TelemetryOptions Telemetry { get; init; }
    public HealthChecksOptions HealthChecks { get; init; } = new();

    // Upstream HTTP services this API calls. Each entry configures its HttpClient and, optionally, a health check.
    public IReadOnlyDictionary<string, HttpDependencyOptions> Dependencies { get; init; } = new Dictionary<string, HttpDependencyOptions>();
}
