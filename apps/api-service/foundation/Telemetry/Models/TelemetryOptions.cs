using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Telemetry.Models;

/// <summary>Settings of the telemetry pipeline: where to send it and what to send.</summary>
[ExcludeFromCodeCoverage]
public record TelemetryOptions
{
    /// <summary>The collector connection.</summary>
    public required TelemetryOtlpOptions Otlp { get; init; }
    /// <summary>Extra attributes attached to everything the service exports, such as the environment or region.</summary>
    public IReadOnlyDictionary<string, string> ResourceAttributes { get; init; } = new Dictionary<string, string>();
    /// <summary>Log export settings.</summary>
    public TelemetryLoggingOptions Logging { get; init; } = new();
    /// <summary>Metric export settings.</summary>
    public TelemetryMetricsOptions Metrics { get; init; } = new();
    /// <summary>Trace export settings.</summary>
    public TelemetryTracingOptions Tracing { get; init; } = new();
}
