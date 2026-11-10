using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Models;

/// <summary>The health of one dependency in the status report.</summary>
public sealed record DependencyReport
{
    /// <summary>The name of the check.</summary>
    public required string Name { get; init; }
    /// <summary>Healthy, degraded or unhealthy.</summary>
    public required HealthStatus Status { get; init; }
    /// <summary>What went wrong, when something did.</summary>
    public string? Description { get; init; }
    /// <summary>How long the check took.</summary>
    public required TimeSpan Duration { get; init; }
    /// <summary>The tags of the check, such as critical or type-database.</summary>
    public required string[] Tags { get; init; }

    /// <summary>Builds the report from a health report entry.</summary>
    public static DependencyReport From(KeyValuePair<string, HealthReportEntry> entry)
    {
        return new()
        {
            Name = entry.Key,
            Status = entry.Value.Status,
            Description = entry.Value.Description,
            Duration = entry.Value.Duration,
            Tags = [.. entry.Value.Tags],
        };
    }
}
