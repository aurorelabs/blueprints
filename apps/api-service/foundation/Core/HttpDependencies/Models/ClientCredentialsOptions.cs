using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.HttpDependencies.Models;

/// <summary>OAuth2 client credentials. For Keycloak, <c>TokenUrl</c> is <c>{authority}/protocol/openid-connect/token</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed record ClientCredentialsOptions
{
    /// <summary>The OAuth2 token endpoint to request tokens from.</summary>
    public string TokenUrl { get; init; } = string.Empty;
    /// <summary>The client id registered at the identity provider.</summary>
    public string ClientId { get; init; } = string.Empty;
    /// <summary>The client secret. Supply it through an environment variable or a secret store.</summary>
    public string ClientSecret { get; init; } = string.Empty;
    /// <summary>The scopes to request.</summary>
    public string[] Scopes { get; init; } = [];
}
