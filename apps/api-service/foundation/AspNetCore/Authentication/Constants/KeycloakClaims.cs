namespace Aurore.Foundation.AspNetCore.Authentication.Constants;

/// <summary>Names of the claims Keycloak puts roles in.</summary>
public static class KeycloakClaims
{
    /// <summary>Holds the realm roles as JSON: realm_access.roles.</summary>
    public const string RealmAccess = "realm_access";
    /// <summary>Holds the client roles as JSON: resource_access.{clientId}.roles.</summary>
    public const string ResourceAccess = "resource_access";
}
