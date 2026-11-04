namespace Aurore.Foundation.Core.HttpDependencies.Models;

/// <summary>OAuth2 client credentials. For Keycloak, <c>TokenUrl</c> is <c>{authority}/protocol/openid-connect/token</c>.</summary>
public sealed record ClientCredentialsOptions
{
    public string TokenUrl { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = [];
}
