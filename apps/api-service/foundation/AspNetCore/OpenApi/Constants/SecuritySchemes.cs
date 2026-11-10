namespace Aurore.Foundation.AspNetCore.OpenApi.Constants;

/// <summary>Names of the security schemes listed in the API document.</summary>
public static class SecuritySchemes
{
    /// <summary>A bearer token pasted into the document.</summary>
    public const string JwtBearer = "Bearer";
    /// <summary>OpenID Connect sign-in.</summary>
    public const string Oidc = "OIDC";
    /// <summary>The OAuth2 authorization code flow.</summary>
    public const string OAuth2 = "OAuth2";
}
