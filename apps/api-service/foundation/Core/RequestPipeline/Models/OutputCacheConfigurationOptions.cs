using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>Settings of the output cache.</summary>
[ExcludeFromCodeCoverage]
public sealed record OutputCacheConfigurationOptions
{
    /// <summary>
    /// Named policies; endpoints opt in with .CacheOutput("&lt;name&gt;"). Nothing is cached unless an endpoint opts in.
    /// </summary>
    public Dictionary<string, OutputCachePolicyOptions> Policies { get; init; } = [];
}
