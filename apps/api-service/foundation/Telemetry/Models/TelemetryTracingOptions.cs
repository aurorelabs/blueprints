namespace Aurore.Foundation.Telemetry.Models;

public record TelemetryTracingOptions
{
    public double TraceIdRatioBasedSampler { get; init; } = 1.0;
    public string[] IncomingTrafficFilters { get; init; } = [];
    public string[] OutgoingTrafficFilters { get; init; } = [];
    public bool ExportToConsole { get; init; } = false;
}
