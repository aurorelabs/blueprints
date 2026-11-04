namespace Aurore.Foundation.Core.Application.Models;

public record AppInformationOptions
{
    /// <summary>
    /// Not configurable: comes from the entry assembly (see AppVersion), so it can't drift from what is deployed.
    /// </summary>
    public string Version { get; } = AppVersion.Current;
    public required string Domain { get; init; }
    public required string Boundary { get; init; }
    public required string Component { get; init; }
    public required string Description { get; init; }
    public required AppMaintainerOptions Maintainer { get; init; }
    public string Locale { get; init; } = "en-US";
    public string Namespace => $"{Domain}.{Boundary}";
    public string ActivitySource => $"{Namespace}.{Component}";
}
