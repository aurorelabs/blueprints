namespace Aurore.Foundation.Core.HttpDependencies.Models;

/// <summary>A static header such as <c>X-Api-Key: &lt;key&gt;</c> or <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
public sealed record UpstreamApiKeyOptions
{
    public string HeaderName { get; init; } = "X-Api-Key";
    public string Value { get; init; } = string.Empty;
}
