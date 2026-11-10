using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Telemetry.Models;

/// <summary>Settings of metric export.</summary>
[ExcludeFromCodeCoverage]
public record TelemetryMetricsOptions
{
    /// <summary>Also write the metrics to the console, which helps when no collector is running.</summary>
    public bool ExportToConsole { get; init; } = false;

    /// <summary>Interval between two metric exports.</summary>
    public int ExportIntervalSeconds { get; init; } = 60;

    /// <summary>Maximum time a metric export may take before being cancelled.</summary>
    public int ExportTimeoutSeconds { get; init; } = 30;
}
