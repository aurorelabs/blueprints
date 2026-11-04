using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Models;

public sealed record DependencyReport
{
    public required string Name { get; init; }
    public required HealthStatus Status { get; init; }
    public string? Description { get; init; }
    public required TimeSpan Duration { get; init; }
    public required string[] Tags { get; init; }

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
