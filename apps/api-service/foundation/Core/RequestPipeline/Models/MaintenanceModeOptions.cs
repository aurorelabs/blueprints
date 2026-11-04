namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record MaintenanceModeOptions
{
    public bool Enabled { get; init; }
    public string? Message { get; init; }
    public int RetryAfterSeconds { get; init; } = 60;
}
