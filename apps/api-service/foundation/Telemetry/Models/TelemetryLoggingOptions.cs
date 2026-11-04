namespace Aurore.Foundation.Telemetry.Models;

public record TelemetryLoggingOptions
{
    public bool IncludeScopes { get; init; } = true;
    public bool ParseStateValues { get; init; } = true;
}
