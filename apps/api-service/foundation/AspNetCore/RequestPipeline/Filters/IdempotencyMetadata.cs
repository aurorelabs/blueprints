namespace Aurore.Foundation.AspNetCore.RequestPipeline.Filters;

/// <summary>
/// Endpoint metadata added by WithIdempotency.
/// </summary>
public sealed record IdempotencyMetadata(bool Required);
