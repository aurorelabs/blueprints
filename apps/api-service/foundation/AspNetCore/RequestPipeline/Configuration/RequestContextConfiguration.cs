using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.Models;
using Aurore.Foundation.Core.Privacy;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;

/// <summary>Registers the request context and the privacy protector, and the steps that fill the context.</summary>
public static class RequestContextConfiguration
{
    internal static IServiceCollection AddAuroreRequestContext(this IServiceCollection services, PrivacyOptions privacy)
    {
        return services
            .AddSingleton<IPrivacyProtector>(new PrivacyProtector(privacy))
            .AddScoped<RequestContext>();
    }

    internal static IApplicationBuilder UseAuroreRequestContext(this IApplicationBuilder builder)
    {
        return builder
            .UseMiddleware<RequestContextMiddleware>();
    }

    /// <summary>
    /// Must run after authentication.
    /// </summary>
    internal static IApplicationBuilder UseAuroreRequestUser(this IApplicationBuilder builder)
    {
        return builder
            .UseMiddleware<RequestUserMiddleware>();
    }
}
