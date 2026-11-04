using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Models;

public sealed record HealthCheckReport
{
    public required HealthStatus Status { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public required DateTime Timestamp { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<DependencyReport>? Dependencies { get; init; }

    public static HealthCheckReport From(HealthReport report, bool includeDependencies)
    {
        return new()
        {
            Status = report.Status,
            TotalDuration = report.TotalDuration,
            Timestamp = DateTime.UtcNow,
            Dependencies = includeDependencies ? report.Entries.Select(DependencyReport.From).ToList() : null,
        };
    }
}
