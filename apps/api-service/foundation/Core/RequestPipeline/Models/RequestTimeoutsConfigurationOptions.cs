using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>Settings of the request timeouts.</summary>
[ExcludeFromCodeCoverage]
public sealed record RequestTimeoutsConfigurationOptions
{
    /// <summary>
    /// Applies to every endpoint unless it uses a named policy (.WithRequestTimeout("&lt;name&gt;")) or .DisableRequestTimeout().
    /// </summary>
    public int DefaultSeconds { get; init; } = 100;

    /// <summary>
    /// Named policies: policy name to timeout in seconds.
    /// </summary>
    public Dictionary<string, int> Policies { get; init; } = [];
}
