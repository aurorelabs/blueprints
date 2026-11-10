using System.Linq;
using Microsoft.AspNetCore.Builder;

namespace Aurore.Foundation.AspNetCore.Authentication.Extensions;

/// <summary>
/// Role and scope requirements for endpoints and resource groups. Each call adds a requirement, and the requirements of
/// every call and every level (group, endpoint) must all pass.
/// </summary>
public static class EndpointAuthorizationExtensions
{
    /// <summary>Requires the caller to have EVERY one of the roles.</summary>
    public static TBuilder RequireRoles<TBuilder>(this TBuilder builder, params string[] roles)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy =>
        {
            foreach (var role in roles)
                policy.RequireRole(role);
        });
    }

    /// <summary>Requires the caller to have AT LEAST ONE of the roles.</summary>
    public static TBuilder RequireAnyRole<TBuilder>(this TBuilder builder, params string[] roles)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy => policy.RequireRole(roles));
    }

    /// <summary>Requires the caller to have EVERY one of the scopes.</summary>
    public static TBuilder RequireScopes<TBuilder>(this TBuilder builder, params string[] scopes)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => scopes.All(context.User.HasScope)));
    }

    /// <summary>Requires the caller to have AT LEAST ONE of the scopes.</summary>
    public static TBuilder RequireAnyScope<TBuilder>(this TBuilder builder, params string[] scopes)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.RequireAuthorization(policy => policy
            .RequireAuthenticatedUser()
            .RequireAssertion(context => scopes.Any(context.User.HasScope)));
    }
}
