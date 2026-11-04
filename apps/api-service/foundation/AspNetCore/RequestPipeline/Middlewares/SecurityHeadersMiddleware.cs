using System.Threading.Tasks;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Http;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";

            if (options.ContentSecurityPolicy.HasValue())
                context.Response.Headers.ContentSecurityPolicy = options.ContentSecurityPolicy;

            return Task.CompletedTask;
        });

        return next(context);
    }
}
