using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>One named output cache policy.</summary>
[ExcludeFromCodeCoverage]
public sealed record OutputCachePolicyOptions
{
    /// <summary>How long a cached response is served.</summary>
    public int ExpirationSeconds { get; init; } = 60;

    /// <summary>
    /// Query keys that produce separate cache entries. "*" (the default) means every key.
    /// </summary>
    public string[] VaryByQuery { get; init; } = ["*"];

    /// <summary>
    /// Request headers that produce separate cache entries. A response cached without varying by the caller is shared by
    /// all callers: add "Authorization" when the response depends on who is asking.
    /// </summary>
    public string[] VaryByHeader { get; init; } = [];
}
