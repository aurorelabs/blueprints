using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Layers;

/// <summary>
/// Shows how conventions stack (application, group, endpoint) and how a version is deprecated.
/// </summary>
internal sealed class LayersResource() : DemoResource("layers", "Convention layers")
{
    internal const string ConfiguredByHeader = "X-Configured-By";

    public override MinimalEndpointVersion[] Versions => [1, new(2, true)];

    public override int[] Problems => [StatusCodes.Status404NotFound, StatusCodes.Status409Conflict, StatusCodes.Status500InternalServerError];

    public override void Configure(RouteGroupBuilder builder)
    {
        builder
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AddEndpointFilter((context, next) =>
            {
                context.HttpContext.Response.Headers[ConfiguredByHeader] = "group";
                context.HttpContext.Response.Headers["X-Level-Group"] = "true";

                return next(context);
            });
    }
}
