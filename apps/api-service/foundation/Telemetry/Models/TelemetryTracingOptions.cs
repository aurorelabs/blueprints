using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Telemetry.Models;

/// <summary>Settings of trace export.</summary>
[ExcludeFromCodeCoverage]
public record TelemetryTracingOptions
{
    /// <summary>
    /// Fraction of traces kept: 1.0 keeps all of them, 0.1 keeps one in ten.
    /// </summary>
    public double SamplingRatio { get; init; } = 1.0;

    /// <summary>
    /// Path prefixes of incoming requests that are not traced (/health/ is always excluded).
    /// </summary>
    public string[] ExcludedIncomingPaths { get; init; } = [];

    /// <summary>
    /// Path prefixes of outgoing HTTP calls that are not traced (/health/ is always excluded).
    /// </summary>
    public string[] ExcludedOutgoingPaths { get; init; } = [];

    /// <summary>Also write the spans to the console, which helps when no collector is running.</summary>
    public bool ExportToConsole { get; init; } = false;
    /// <summary>How spans are batched before export.</summary>
    public TelemetryBatchExportOptions BatchExport { get; init; } = new();
}
