using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Telemetry.Models;

/// <summary>Settings of log export.</summary>
[ExcludeFromCodeCoverage]
public record TelemetryLoggingOptions
{
    /// <summary>Whether log scopes (correlation id, user id and so on) are exported with each log record.</summary>
    public bool IncludeScopes { get; init; } = true;
    /// <summary>Whether structured values of the log message are exported as attributes.</summary>
    public bool ParseStateValues { get; init; } = true;
    /// <summary>How log records are batched before export.</summary>
    public TelemetryBatchExportOptions BatchExport { get; init; } = new();
}
