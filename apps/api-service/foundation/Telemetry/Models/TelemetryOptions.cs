using System.Collections.Generic;

namespace Aurore.Foundation.Telemetry.Models;

public record TelemetryOptions
{
    public required TelemetryOtlpOptions Otlp { get; init; }
    public IReadOnlyDictionary<string, string> ResourceAttributes { get; init; } = new Dictionary<string, string>();
    public TelemetryLoggingOptions Logging { get; init; } = new();
    public TelemetryMetricsOptions Metrics { get; init; } = new();
    public TelemetryTracingOptions Tracing { get; init; } = new();
}
