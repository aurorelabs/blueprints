using Aurore.Foundation.AspNetCore.MinimalApis.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Aurore.Blueprints.ApiService.App.Api.Configuration;

internal static class ResourcesConfiguration
{
    internal const string ConfiguredByHeader = "X-Configured-By";

    internal static WebApplication MapAppResources(this WebApplication app)
    {
        app.MapAuroreResourcesFromAssembly<Program>(api => api
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)

            // Layering demo (see Examples.http); remove when no longer needed.
            .AddEndpointFilter((context, next) =>
            {
                context.HttpContext.Response.Headers[ConfiguredByHeader] = "app";
                context.HttpContext.Response.Headers["X-Level-App"] = "true";

                return next(context);
            }));

        return app;
    }
}
