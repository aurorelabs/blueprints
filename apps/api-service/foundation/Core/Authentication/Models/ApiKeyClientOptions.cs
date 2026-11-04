namespace Aurore.Foundation.Core.Authentication.Models;

public sealed record ApiKeyClientOptions
{
    public string Key { get; init; } = string.Empty;
    public string[] Roles { get; init; } = [];
    public string[] Scopes { get; init; } = [];
}
