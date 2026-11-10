using System.Security.Claims;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.AspNetCore.Authentication.Extensions;

[Trait("Feature", "Endpoint authorization")]
public sealed class EndpointAuthorizationExtensionsTests
{
    private static readonly string[] _required = ["first", "second"];

    public static TheoryData<string, string[], bool> RoleCases => new()
    {
        { nameof(EndpointAuthorizationExtensions.RequireRoles), ["first", "second"], true },
        { nameof(EndpointAuthorizationExtensions.RequireRoles), ["first"], false },
        { nameof(EndpointAuthorizationExtensions.RequireAnyRole), ["first"], true },
        { nameof(EndpointAuthorizationExtensions.RequireAnyRole), ["other"], false },
    };

    public static TheoryData<string, string[], bool> ScopeCases => new()
    {
        { nameof(EndpointAuthorizationExtensions.RequireScopes), ["first", "second"], true },
        { nameof(EndpointAuthorizationExtensions.RequireScopes), ["first"], false },
        { nameof(EndpointAuthorizationExtensions.RequireAnyScope), ["first"], true },
        { nameof(EndpointAuthorizationExtensions.RequireAnyScope), ["other"], false },
    };

    [Theory(DisplayName = "Roles: requiring all needs every role, requiring any needs one")]
    [MemberData(nameof(RoleCases))]
    public async Task RoleRequirementsBehaveAsDocumented(string method, string[] granted, bool expected)
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(granted.Select(x => new Claim(ClaimTypes.Role, x)), "test"));

        // Act
        var allowed = await IsAllowed(method, user);

        // Assert
        Assert.Equal(expected, allowed);
    }

    [Theory(DisplayName = "Scopes: requiring all needs every scope, requiring any needs one")]
    [MemberData(nameof(ScopeCases))]
    public async Task ScopeRequirementsBehaveAsDocumented(string method, string[] granted, bool expected)
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("scope", string.Join(' ', granted))], "test"));

        // Act
        var allowed = await IsAllowed(method, user);

        // Assert
        Assert.Equal(expected, allowed);
    }

    [Fact(DisplayName = "An anonymous caller never passes a scope requirement")]
    public async Task AnonymousCallerIsRejected()
    {
        // Arrange
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var allowed = await IsAllowed(nameof(EndpointAuthorizationExtensions.RequireAnyScope), anonymous);

        // Assert
        Assert.False(allowed);
    }

    private static async Task<bool> IsAllowed(string method, ClaimsPrincipal user)
    {
        var builder = new RecordingBuilder();

        _ = method switch
        {
            nameof(EndpointAuthorizationExtensions.RequireRoles) => builder.RequireRoles(_required),
            nameof(EndpointAuthorizationExtensions.RequireAnyRole) => builder.RequireAnyRole(_required),
            nameof(EndpointAuthorizationExtensions.RequireScopes) => builder.RequireScopes(_required),
            _ => builder.RequireAnyScope(_required),
        };

        var endpoint = new RouteEndpointBuilder(_ => Task.CompletedTask, RoutePatternFactory.Parse("/"), 0);
        builder.Apply(endpoint);

        using var provider = new ServiceCollection().AddLogging().AddAuthorization().BuildServiceProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        foreach (var policy in endpoint.Metadata.OfType<AuthorizationPolicy>())
        {
            if ((await authorization.AuthorizeAsync(user, policy)).Succeeded is false)
                return false;
        }

        return true;
    }

    private sealed class RecordingBuilder : IEndpointConventionBuilder
    {
        private readonly List<Action<EndpointBuilder>> _conventions = [];

        public void Add(Action<EndpointBuilder> convention)
        {
            _conventions.Add(convention);
        }

        public void Finally(Action<EndpointBuilder> finallyConvention)
        {
        }

        public void Apply(EndpointBuilder endpoint)
        {
            foreach (var convention in _conventions)
                convention(endpoint);
        }
    }
}
