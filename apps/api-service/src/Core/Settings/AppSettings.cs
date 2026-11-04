using System.Collections.Generic;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.HealthChecks.Models;
using Aurore.Foundation.Core.HttpDependencies.Models;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Aurore.Foundation.Telemetry.Models;

namespace Aurore.Blueprints.ApiService.Core.Settings;

public sealed record AppSettings
{
    public required AppInformationOptions AppInformation { get; init; }
    public IReadOnlyDictionary<string, CorsPolicyOptions> CorsPolicies { get; init; } = new Dictionary<string, CorsPolicyOptions>();
    public required IdentityProviderOptions IdentityProvider { get; init; }
    public ApiKeysOptions ApiKeys { get; init; } = new();
    public required TelemetryOptions Telemetry { get; init; }
    public HealthChecksOptions HealthChecks { get; init; } = new();
    public RequestPipelineOptions RequestPipeline { get; init; } = new();
    public MaintenanceModeOptions MaintenanceMode { get; init; } = new();
    public IReadOnlyDictionary<string, HttpDependencyOptions> Dependencies { get; init; } = new Dictionary<string, HttpDependencyOptions>();
}
