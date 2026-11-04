using System;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Models;

public sealed record LivenessReport
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required DateTime Timestamp { get; init; }

    public static LivenessReport Create(string name, string version)
    {
        return new()
        {
            Name = name,
            Version = version,
            Timestamp = DateTime.UtcNow,
        };
    }
}
