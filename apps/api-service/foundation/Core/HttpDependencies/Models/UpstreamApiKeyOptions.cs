using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.HttpDependencies.Models;

/// <summary>A static header such as <c>X-Api-Key: &lt;key&gt;</c> or <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record UpstreamApiKeyOptions
{
    /// <summary>The header that carries the value.</summary>
    public string HeaderName { get; init; } = "X-Api-Key";
    /// <summary>
    /// The header value: the key or token. Supply it through an environment variable or a secret store.
    /// </summary>
    public string Value { get; init; } = string.Empty;
}
