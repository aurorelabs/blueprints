using System.Threading.Tasks;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Http;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;

/// <summary>
/// Adds the security headers to every response: no content sniffing, no framing, no referrer, and the content security
/// policy when one is configured.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, SecurityHeadersOptions options)
{
    /// <summary>Adds the headers when the response starts.</summary>
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
