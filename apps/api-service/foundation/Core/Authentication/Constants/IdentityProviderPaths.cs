namespace Aurore.Foundation.Core.Authentication.Constants;

public static class IdentityProviderPaths
{
    public static class Keycloak
    {
        public const string Authorization = "protocol/openid-connect/auth";
        public const string Token = "protocol/openid-connect/token";
    }

    /// <summary>
    /// Common OAuth2 naming. OIDC does not standardize these paths: the authoritative values are in the
    /// provider's /.well-known/openid-configuration document (authorization_endpoint, token_endpoint).
    /// </summary>
    public static class Standard
    {
        public const string Authorization = "authorize";
        public const string Token = "token";
    }
}
