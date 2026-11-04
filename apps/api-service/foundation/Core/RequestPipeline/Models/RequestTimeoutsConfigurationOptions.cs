using System.Collections.Generic;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

public sealed record RequestTimeoutsConfigurationOptions
{
    /// <summary>
    /// Applies to every endpoint unless it uses a named policy (.WithRequestTimeout("&lt;name&gt;")) or .DisableRequestTimeout().
    /// </summary>
    public int DefaultSeconds { get; init; } = 100;
    public Dictionary<string, int> Policies { get; init; } = [];
}
