using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Telemetry.Models;

/// <summary>
/// Batching behavior for log and trace export. Defaults match the OpenTelemetry SDK defaults.
/// </summary>
[ExcludeFromCodeCoverage]
public record TelemetryBatchExportOptions
{
    /// <summary>Records buffered in memory; when full, new records are dropped.</summary>
    public int MaxQueueSize { get; init; } = 2048;

    /// <summary>Maximum records sent per export request. Must not exceed <see cref="MaxQueueSize"/>.</summary>
    public int MaxExportBatchSize { get; init; } = 512;

    /// <summary>Delay between two consecutive exports.</summary>
    public int ScheduledDelaySeconds { get; init; } = 5;

    /// <summary>Maximum time an export may take before being cancelled.</summary>
    public int ExportTimeoutSeconds { get; init; } = 30;
}
