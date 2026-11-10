using Aurore.Blueprints.ApiService.App.Api.Demo.Layers;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.MinimalApis.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Aurore.Blueprints.ApiService.App.Api.Configuration;

internal static class ResourcesConfiguration
{
    internal static WebApplication MapAppResources(this WebApplication app, MinimalEndpointCatalog endpoints)
    {
        app.MapAuroreMinimalEndpoints(endpoints, api => api
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)

            // Layering demo (see Examples.http); remove when no longer needed.
            .AddEndpointFilter((context, next) =>
            {
                context.HttpContext.Response.Headers[LayersResource.ConfiguredByHeader] = "app";
                context.HttpContext.Response.Headers["X-Level-App"] = "true";

                return next(context);
            }));

        return app;
    }
}
