using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>One named rate limit policy: a fixed window per caller.</summary>
[ExcludeFromCodeCoverage]
public sealed record RateLimitPolicyOptions
{
    /// <summary>How many requests a caller may make in one window.</summary>
    public int PermitLimit { get; init; } = 100;
    /// <summary>The length of the window.</summary>
    public int WindowSeconds { get; init; } = 60;
}
