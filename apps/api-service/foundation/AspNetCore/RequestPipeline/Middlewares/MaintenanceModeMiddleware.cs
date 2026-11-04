using System.Globalization;
using System.Threading.Tasks;
using Aurore.Foundation.Core.HealthChecks.Constants;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Http;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;

public sealed class MaintenanceModeMiddleware(RequestDelegate next, MaintenanceModeOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(HealthCheckProperties.Endpoints.Base))
        {
            await next(context);

            return;
        }

        context.Response.Headers.RetryAfter = options.RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        await TypedResults
            .Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Service Unavailable",
                detail: options.Message ?? "The service is temporarily unavailable for maintenance.")
            .ExecuteAsync(context);
    }
}
