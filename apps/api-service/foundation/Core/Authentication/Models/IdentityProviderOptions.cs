using System.Collections.Generic;
using Aurore.Foundation.Core.Authentication.Constants;

namespace Aurore.Foundation.Core.Authentication.Models;

public sealed record IdentityProviderOptions
{
    public required string AuthorityUrl { get; init; }
    public required string Audience { get; init; }
    public Dictionary<string, string> Scopes { get; init; } = [];
    public bool RequireHttpsMetadata { get; init; } = true;

    /// <summary>
    /// Paths relative to AuthorityUrl. Defaults are Keycloak's; use IdentityProviderPaths for other presets or set any value.
    /// </summary>
    public string AuthorizationPath { get; init; } = IdentityProviderPaths.Keycloak.Authorization;
    public string TokenPath { get; init; } = IdentityProviderPaths.Keycloak.Token;
}
