using System.Security.Claims;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;

namespace Tests.AspNetCore.Authentication.Extensions;

[Trait("Feature", "Claims helpers")]
public sealed class ClaimsPrincipalExtensionsTests
{
    public static TheoryData<string> ScopeClaimTypes =>
    [
        "scope",
        "scp",
        "http://schemas.microsoft.com/identity/claims/scope",
    ];

    [Theory(DisplayName = "The space-separated scopes are split, whichever claim type the provider uses")]
    [MemberData(nameof(ScopeClaimTypes))]
    public void ScopesAreSplit(string claimType)
    {
        // Arrange
        var user = User((claimType, "api.read api.write"));

        // Act
        var scopes = user.GetScopes();

        // Assert
        Assert.Equal(["api.read", "api.write"], scopes.Order());
    }

    [Fact(DisplayName = "A scope that only shares a prefix with a granted one is not granted")]
    public void ScopeMatchIsExact()
    {
        // Arrange
        var user = User(("scope", "api.readonly"));

        // Act
        var has = user.HasScope("api.read");

        // Assert
        Assert.False(has);
    }

    [Fact(DisplayName = "The roles are the role claims of the caller")]
    public void RolesAreTheRoleClaims()
    {
        // Arrange
        var user = User((ClaimTypes.Role, "admin"), (ClaimTypes.Role, "reader"));

        // Act
        var roles = user.GetRoles();

        // Assert
        Assert.Equal(["admin", "reader"], roles.Order());
        Assert.True(user.HasRole("admin"));
        Assert.False(user.HasRole("owner"));
    }

    private static ClaimsPrincipal User(params (string Type, string Value)[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims.Select(x => new Claim(x.Type, x.Value)), "test"));
    }
}
