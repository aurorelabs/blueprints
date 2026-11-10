using System.Security.Claims;
using Aurore.Foundation.AspNetCore.Authentication.Services;
using Aurore.Foundation.Core.Authentication.Models;

namespace Tests.AspNetCore.Authentication.Services;

[Trait("Feature", "Role claims")]
public sealed class RoleClaimsMapperTests
{
    private const string RealmAccess = """{"roles":["admin","offline_access"]}""";
    private const string ResourceAccess = """{"api":{"roles":["reader"]},"other":{"roles":["secret"]}}""";

    [Fact(DisplayName = "Keycloak realm roles and the roles of the audience's client become role claims")]
    public void KeycloakRolesAreMapped()
    {
        // Arrange
        var identity = Identity(("realm_access", RealmAccess), ("resource_access", ResourceAccess));

        // Act
        RoleClaimsMapper.Map(identity, IdentityProviderKind.Keycloak, "api");

        // Assert
        Assert.Equal(["admin", "offline_access", "reader"], Roles(identity).Order());
    }

    [Fact(DisplayName = "Keycloak roles of other clients are not granted")]
    public void RolesOfOtherClientsAreIgnored()
    {
        // Arrange
        var identity = Identity(("resource_access", ResourceAccess));

        // Act
        RoleClaimsMapper.Map(identity, IdentityProviderKind.Keycloak, "api");

        // Assert
        Assert.DoesNotContain("secret", Roles(identity));
    }

    [Fact(DisplayName = "A generic provider's role and roles claims become role claims")]
    public void GenericRolesAreMapped()
    {
        // Arrange
        var identity = Identity(("role", "admin"), ("roles", "reader"));

        // Act
        RoleClaimsMapper.Map(identity, IdentityProviderKind.Generic, "api");

        // Assert
        Assert.Equal(["admin", "reader"], Roles(identity).Order());
    }

    [Fact(DisplayName = "A generic provider ignores Keycloak's JSON claims, so a look-alike claim grants nothing")]
    public void GenericIgnoresKeycloakClaims()
    {
        // Arrange
        var identity = Identity(("realm_access", RealmAccess));

        // Act
        RoleClaimsMapper.Map(identity, IdentityProviderKind.Generic, "api");

        // Assert
        Assert.Empty(Roles(identity));
    }

    [Fact(DisplayName = "A role the identity already has is not added twice")]
    public void ExistingRolesAreNotDuplicated()
    {
        // Arrange
        var identity = Identity((ClaimTypes.Role, "admin"), ("roles", "admin"));

        // Act
        RoleClaimsMapper.Map(identity, IdentityProviderKind.Generic, "api");

        // Assert
        Assert.Single(Roles(identity));
    }

    [Theory(DisplayName = "A malformed Keycloak claim is ignored instead of failing the request")]
    [InlineData("not json")]
    [InlineData("""{"roles":"admin"}""")]
    [InlineData("[]")]
    public void MalformedKeycloakClaimsAreIgnored(string value)
    {
        // Arrange
        var identity = Identity(("realm_access", value), ("resource_access", value));

        // Act
        RoleClaimsMapper.Map(identity, IdentityProviderKind.Keycloak, "api");

        // Assert
        Assert.Empty(Roles(identity));
    }

    private static ClaimsIdentity Identity(params (string Type, string Value)[] claims)
    {
        return new ClaimsIdentity(claims.Select(x => new Claim(x.Type, x.Value)), "test");
    }

    private static IEnumerable<string> Roles(ClaimsIdentity identity)
    {
        return identity.FindAll(ClaimTypes.Role).Select(x => x.Value);
    }
}
