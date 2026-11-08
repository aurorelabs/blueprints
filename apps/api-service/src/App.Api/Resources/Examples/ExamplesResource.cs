using Aurore.Blueprints.ApiService.App.Api.Configuration;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Resources.Examples;

internal sealed class ExamplesResource : ResourceGroup
{
    public override string Name => "examples";

    public override bool RequireAuthorization => true;

    public override string[] Tags => ["Examples"];

    public override MinimalEndpointVersion[] Versions => [1, new(2, true)];

    public override int[] Problems => [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError];

    public override void Configure(RouteGroupBuilder builder)
    {
        builder
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter((context, next) =>
            {
                context.HttpContext.Response.Headers[ResourcesConfiguration.ConfiguredByHeader] = "group";
                context.HttpContext.Response.Headers["X-Level-Group"] = "true";

                return next(context);
            });
    }
}
