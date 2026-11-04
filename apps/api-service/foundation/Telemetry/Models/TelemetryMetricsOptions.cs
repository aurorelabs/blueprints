namespace Aurore.Foundation.Telemetry.Models;

public record TelemetryMetricsOptions
{
    public bool ExportToConsole { get; init; } = false;
}
