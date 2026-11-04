using System.Collections.Generic;
using Aurore.Foundation.Core;
using Aurore.Foundation.Telemetry;

namespace Aurore.Blueprints.ApiService.Core;

public sealed record AppSettings
{
    public required AppInformationOptions AppInformation { get; init; }
    public required IReadOnlyDictionary<string, CorsPolicyOptions> CorsPolicies { get; init; }
    public required IdentityProviderOptions IdentityProvider { get; init; }
    public required TelemetryOptions Telemetry { get; init; }
    public Dictionary<string, HttpServiceDependencyOptions> Services { get; init; } = []; // AI: Maybe improve this name?
    public required SecuritySettings Security { get; init; }

    public HealthCheckEndpointOptions GetHealthCheckEndpointOptions()
    {
        return new()
        {
            ServiceName = AppInformation.Component,
            Version = AppInformation.Version,
            Token = Security.HealthCheck.Token
        };
    }
}

public sealed record SecuritySettings
{
    public HealthCheckConfigurationOptions HealthCheck { get; init; } = new();
}
