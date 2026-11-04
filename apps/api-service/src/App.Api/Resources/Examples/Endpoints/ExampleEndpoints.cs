using System.Linq;
using System.Security.Claims;
using Aurore.Blueprints.ApiService.App.Api.Configuration;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Resources.Examples.Endpoints;

internal sealed class GetLayersEndpoint : MinimalEndpoint<ExamplesResource>
{
    public override string Summary => "Shows how app, group and endpoint conventions stack";

    public override string Description => "Anonymous endpoint returning headers written by each level.";

    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("layers", () => TypedResults.Ok(new { Message = "Check the X-Level-* and X-Configured-By response headers." }))
            .AllowAnonymous()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter((context, next) =>
            {
                context.HttpContext.Response.Headers[ResourcesConfiguration.ConfiguredByHeader] = "endpoint";
                context.HttpContext.Response.Headers["X-Level-Endpoint"] = "true";

                return next(context);
            });
    }
}

internal sealed class GetLayersV2Endpoint : MinimalEndpoint<ExamplesResource>
{
    public override int Version => 2;

    public override string Name => "GetLayersV2";

    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("layers", () => TypedResults.Ok(new { Message = "Version 2 of the same route." }))
            .AllowAnonymous();
    }
}

internal sealed class GetAuthenticatedEndpoint : MinimalEndpoint<ExamplesResource>
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

internal sealed class GetScopeEndpoint : MinimalEndpoint<ExamplesResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("scope", () => TypedResults.Ok(new { Granted = "api.read" }))
            .RequireAuthorization(policy => policy.RequireAuroreScope("api.read"));
    }
}

internal sealed class GetRoleEndpoint : MinimalEndpoint<ExamplesResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("role", () => TypedResults.Ok(new { Granted = "admin" }))
            .RequireAuthorization(policy => policy.RequireAuroreRole("admin"));
    }
}

internal sealed class GetClientRoleEndpoint : MinimalEndpoint<ExamplesResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("client-role", () => TypedResults.Ok(new { Granted = "api:reader" }))
            .RequireAuthorization(policy => policy.RequireAuroreKeycloakClientRole("api", "reader"));
    }
}

internal sealed class GetCombinedEndpoint : MinimalEndpoint<ExamplesResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("combined", () => TypedResults.Ok(new { Granted = "api.write + admin" }))
            .RequireAuthorization(policy => policy.RequireAuroreScope("api.write"))
            .RequireAuthorization(policy => policy.RequireAuroreRole("admin"));
    }
}

internal sealed class GetEitherSchemeEndpoint : MinimalEndpoint<ExamplesResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("either", Handle).RequireAuthorization(policy => policy.RequireAuroreScope("api.read"));

        static IResult Handle(ClaimsPrincipal user)
        {
            return TypedResults.Ok(new { Caller = user.Identity?.Name, Scheme = user.Identity?.AuthenticationType });
        }
    }
}

internal sealed class GetApiKeyOnlyEndpoint : MinimalEndpoint<ExamplesResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("api-key-only", (ClaimsPrincipal user) => TypedResults.Ok(new { Client = user.Identity?.Name }))
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(ApiKeyDefaults.AuthenticationScheme)
                .RequireAuroreRole("reader"));
    }
}

internal sealed class GetBearerOnlyEndpoint : MinimalEndpoint<ExamplesResource>
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
