using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Application.Models;

/// <summary>
/// Who this service is: the names used for telemetry resources, the activity source and the OpenAPI document.
/// </summary>
[ExcludeFromCodeCoverage]
public record AppInformationOptions
{
    /// <summary>
    /// Not configurable: comes from the entry assembly (see AppVersion), so it can't drift from what is deployed.
    /// </summary>
    public string Version { get; } = AppVersion.Current;
    /// <summary>The business domain the service belongs to, the first part of the namespace.</summary>
    public required string Domain { get; init; }
    /// <summary>The bounded context inside the domain, the second part of the namespace.</summary>
    public required string Boundary { get; init; }
    /// <summary>The name of this service or component inside the boundary.</summary>
    public required string Component { get; init; }
    /// <summary>A short description of what the service does.</summary>
    public required string Description { get; init; }
    /// <summary>The team or person to contact about this service.</summary>
    public required AppMaintainerOptions Maintainer { get; init; }
    /// <summary>The default locale of the service, for example en-US.</summary>
    public string Locale { get; init; } = "en-US";
    /// <summary>The domain and boundary together, for example "acme.billing".</summary>
    public string Namespace => $"{Domain}.{Boundary}";
    /// <summary>The name of the application's ActivitySource: the namespace followed by the component.</summary>
    public string ActivitySource => $"{Namespace}.{Component}";
}
