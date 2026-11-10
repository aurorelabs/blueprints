using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Layers;

internal sealed class GetHeadersEndpoint : MinimalEndpoint<LayersResource>
{
    public override string Summary => "Shows how app, group and endpoint conventions stack";

    public override string Description => "Anonymous endpoint returning headers written by each level.";

    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("headers", () => TypedResults.Ok(new { Message = "Check the X-Level-* and X-Configured-By response headers." }))
            .AllowAnonymous()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter((context, next) =>
            {
                context.HttpContext.Response.Headers[LayersResource.ConfiguredByHeader] = "endpoint";
                context.HttpContext.Response.Headers["X-Level-Endpoint"] = "true";

                return next(context);
            });
    }
}

internal sealed class GetHeadersV2Endpoint : MinimalEndpoint<LayersResource>
{
    public override int Version => 2;

    public override string Name => "GetHeadersV2";

    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("headers", () => TypedResults.Ok(new { Message = "Version 2 of the same route." }))
            .AllowAnonymous();
    }
}
