using System.Collections.Generic;
using Aurore.Foundation.Core.Authentication.Constants;
using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Authentication.Models;

/// <summary>
/// The identity provider that issues the tokens this service accepts, and the endpoints used by the OpenAPI UI to sign
/// in.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record IdentityProviderOptions
{
    /// <summary>The base URL of the identity provider (the token issuer).</summary>
    public required string AuthorityUrl { get; init; }
    /// <summary>The audience a token must be issued for.</summary>
    public required string Audience { get; init; }
    /// <summary>The scopes the OpenAPI UI can request: scope name to description.</summary>
    public Dictionary<string, string> Scopes { get; init; } = [];
    /// <summary>
    /// Whether the provider's metadata must be fetched over HTTPS. Turn off only for local development.
    /// </summary>
    public bool RequireHttpsMetadata { get; init; } = true;

    /// <summary>
    /// The kind of provider. It decides where roles are read from; every kind ends up as standard role claims, so
    /// RequireRoles and User.IsInRole work the same for all of them.
    /// </summary>
    public IdentityProviderKind Provider { get; init; } = IdentityProviderKind.Keycloak;

    /// <summary>
    /// How many seconds past its expiry a token is still accepted, to absorb clock drift between servers. The framework
    /// default is 300.
    /// </summary>
    public int ClockSkewSeconds { get; init; } = 30;

    /// <summary>
    /// Paths relative to AuthorityUrl. Defaults are Keycloak's; use IdentityProviderPaths for other presets or set any value.
    /// </summary>
    public string AuthorizationPath { get; init; } = IdentityProviderPaths.Keycloak.Authorization;
    /// <summary>The token endpoint path, relative to AuthorityUrl.</summary>
    public string TokenPath { get; init; } = IdentityProviderPaths.Keycloak.Token;
}
