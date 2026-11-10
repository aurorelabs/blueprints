namespace Aurore.Foundation.Core.Authentication.Models;

/// <summary>Which kind of identity provider issues the tokens; decides where the roles are read from.</summary>
public enum IdentityProviderKind
{
    /// <summary>
    /// Keycloak: realm roles in realm_access.roles and client roles in resource_access.{audience}.roles, both as JSON inside
    /// one claim.
    /// </summary>
    Keycloak,

    /// <summary>Any other provider (Entra, Auth0...): roles arrive as plain "role" or "roles" claims.</summary>
    Generic,
}
