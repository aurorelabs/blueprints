namespace Aurore.Foundation.Core.Authentication.Constants;

/// <summary>Endpoint paths, relative to the authority URL, of common identity providers.</summary>
public static class IdentityProviderPaths
{
    /// <summary>The paths Keycloak uses.</summary>
    public static class Keycloak
    {
        /// <summary>The authorization endpoint of a Keycloak realm.</summary>
        public const string Authorization = "protocol/openid-connect/auth";
        /// <summary>The token endpoint of a Keycloak realm.</summary>
        public const string Token = "protocol/openid-connect/token";
    }

    /// <summary>
    /// Common OAuth2 naming. OIDC does not standardize these paths: the authoritative values are in the
    /// provider's /.well-known/openid-configuration document (authorization_endpoint, token_endpoint).
    /// </summary>
    public static class Standard
    {
        /// <summary>The common name of the OAuth2 authorization endpoint.</summary>
        public const string Authorization = "authorize";
        /// <summary>The common name of the OAuth2 token endpoint.</summary>
        public const string Token = "token";
    }
}
