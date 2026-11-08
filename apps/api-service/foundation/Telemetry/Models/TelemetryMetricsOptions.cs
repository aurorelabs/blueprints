namespace Aurore.Foundation.Telemetry.Models;

public record TelemetryMetricsOptions
{
    public bool ExportToConsole { get; init; } = false;

    /// <summary>Interval between two metric exports.</summary>
    public int ExportIntervalSeconds { get; init; } = 60;

    /// <summary>Maximum time a metric export may take before being cancelled.</summary>
    public int ExportTimeoutSeconds { get; init; } = 30;
}
