namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record MaintenanceModeOptions
{
    public bool Enabled { get; init; }
    public string? Message { get; init; }

    /// <summary>
    /// Sent to clients in the Retry-After header.
    /// </summary>
    public int RetryAfterSeconds { get; init; } = 60;
}
