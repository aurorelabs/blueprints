using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Telemetry.Models;

/// <summary>
/// The connection to the OpenTelemetry collector, shared by logs, metrics and traces.
/// </summary>
[ExcludeFromCodeCoverage]
public record TelemetryOtlpOptions
{
    /// <summary>
    /// Base URL of the collector, e.g. http://collector:4317 for gRPC or http://collector:4318 for HTTP. For HTTP the
    /// per-signal path (/v1/traces...) is added for you.
    /// </summary>
    public required string Endpoint { get; init; }

    /// <summary>Grpc or HttpProtobuf.</summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// Extra headers sent with every export as comma-separated key=value pairs (e.g. an authentication token). Keep secrets
    /// in environment variables or a secret store.
    /// </summary>
    public string Headers { get; init; } = string.Empty;

    /// <summary>Timeout of each request to the collector.</summary>
    public int RequestTimeoutSeconds { get; init; } = 10;
}
