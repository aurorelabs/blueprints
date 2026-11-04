using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;

public static class RequestContextConfiguration
{
    public static IServiceCollection AddAuroreRequestContext(this IServiceCollection services)
    {
        return services
            .AddScoped<RequestContext>();
    }

    public static IApplicationBuilder UseAuroreRequestContext(this IApplicationBuilder builder)
    {
        return builder
            .UseMiddleware<RequestContextMiddleware>();
    }
}
