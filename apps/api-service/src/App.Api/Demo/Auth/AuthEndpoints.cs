using System.Linq;
using System.Security.Claims;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Auth;

internal sealed class GetAuthenticatedEndpoint : MinimalEndpoint<AuthResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("authenticated", Handle);

        static IResult Handle(ClaimsPrincipal user)
        {
            return TypedResults.Ok(new
            {
                Subject = user.FindFirstValue(ClaimTypes.NameIdentifier),
                Claims = user.Claims.Select(x => new { x.Type, x.Value }),
            });
        }
    }
}

internal sealed class GetScopeEndpoint : MinimalEndpoint<AuthResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("scope", () => TypedResults.Ok(new { Granted = "api.read" }))
            .RequireScopes("api.read");
    }
}

internal sealed class GetRoleEndpoint : MinimalEndpoint<AuthResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("role", () => TypedResults.Ok(new { Granted = "admin" }))
            .RequireRoles("admin");
    }
}

internal sealed class GetCombinedEndpoint : MinimalEndpoint<AuthResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("combined", () => TypedResults.Ok(new { Granted = "api.write + admin" }))
            .RequireScopes("api.write")
            .RequireRoles("admin");
    }
}

internal sealed class GetEitherSchemeEndpoint : MinimalEndpoint<AuthResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("either", Handle).RequireScopes("api.read");

        static IResult Handle(ClaimsPrincipal user)
        {
            return TypedResults.Ok(new { Caller = user.Identity?.Name, Scheme = user.Identity?.AuthenticationType });
        }
    }
}

internal sealed class GetApiKeyOnlyEndpoint : MinimalEndpoint<AuthResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("api-key-only", (ClaimsPrincipal user) => TypedResults.Ok(new { Client = user.Identity?.Name }))
            .RequireAuthorization(policy => policy.AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme).RequireAuthenticatedUser())
            .RequireRoles("reader");
    }
}

internal sealed class GetBearerOnlyEndpoint : MinimalEndpoint<AuthResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("bearer-only", (ClaimsPrincipal user) => TypedResults.Ok(new { Subject = user.FindFirstValue(ClaimTypes.NameIdentifier) }))
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
    }
}
