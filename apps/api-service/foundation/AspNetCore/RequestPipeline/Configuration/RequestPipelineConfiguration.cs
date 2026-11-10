using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.RequestPipeline.Filters;
using Aurore.Foundation.AspNetCore.RequestPipeline.Handlers;
using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;

/// <summary>
/// The request pipeline in three steps; authentication stays in the application, in the middle:
///
///   builder.AddAuroreRequestPipeline(options);       registers everything
///
///   app.UseAuroreRequestPipeline(options);           1. forwarded headers   real client IP and scheme from the reverse proxy
///                                                    2. request context     correlation id and log scope (before the handler, so errors carry them)
///                                                    3. exception handler   unhandled exception to a 500 problem; status code pages give empty 401/403/404/405 a problem body
///                                                    4. security headers
///                                                    5. response compression
///                                                    6. CORS
///   app.UseAuthentication().UseAuthorization();      (application)
///   app.UseAuroreEndpointPolicies();                 7. request user (user id into context, logs, spans), 8. rate limiter (needs the user), 9. request timeouts, 10. output cache
/// </summary>
public static class RequestPipelineConfiguration
{
    /// <summary>
    /// Registers everything the request pipeline needs: limits, forwarded headers, request context, compression, error
    /// handling, timeouts, rate limiting, output cache and idempotency.
    /// </summary>
    public static IHostApplicationBuilder AddAuroreRequestPipeline(this IHostApplicationBuilder builder, RequestPipelineOptions options)
    {
        builder.AddAuroreCorsPolicies(options.CorsPolicies);

        builder.Services
            .ConfigureAuroreKestrelLimits(options.KestrelLimits)
            .ConfigureAuroreForwardedHeaders(options.ForwardedHeaders)
            .AddAuroreRequestContext(options.Privacy)
            .AddAuroreResponseCompression(options.ResponseCompression)
            .AddAuroreExceptionHandler()
            .AddAuroreRequestTimeouts(options.Timeouts)
            .AddAuroreRateLimiting(options.RateLimiting)
            .AddAuroreOutputCache(options.OutputCache)
            .AddAuroreIdempotency(options.Idempotency);

        return builder;
    }

    /// <summary>
    /// Everything that must run before authentication.
    /// </summary>
    public static IApplicationBuilder UseAuroreRequestPipeline(this IApplicationBuilder builder, RequestPipelineOptions options)
    {
        return builder
            .UseForwardedHeaders()
            .UseAuroreRequestContext()
            .UseExceptionHandler()
            .UseStatusCodePages()
            .UseAuroreSecurityHeaders(options.SecurityHeaders)
            .UseAuroreResponseCompression(options.ResponseCompression)
            .UseAuroreCors(options.CorsPolicies);
    }

    /// <summary>
    /// Request user, rate limiting, timeouts and output cache. Must run after authentication: the rate limiter partitions by user.
    /// Endpoints opt in to named policies with .RequireRateLimiting("name"), .WithRequestTimeout("name") and .CacheOutput("name").
    /// </summary>
    public static IApplicationBuilder UseAuroreEndpointPolicies(this IApplicationBuilder builder)
    {
        return builder
            .UseAuroreRequestUser()
            .UseRateLimiter()
            .UseRequestTimeouts()
            .UseOutputCache();
    }

    /// <summary>
    /// Endpoints opt in with .WithIdempotency(); requests carrying an Idempotency-Key header replay the first successful result.
    /// With required: true a request without the header gets 400.
    /// </summary>
    public static TBuilder WithIdempotency<TBuilder>(this TBuilder builder, bool required = false)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder
            .WithMetadata(new IdempotencyMetadata(required))
            .AddEndpointFilter<TBuilder, IdempotencyFilter>();
    }

    internal static IServiceCollection ConfigureAuroreForwardedHeaders(this IServiceCollection services, ForwardedHeadersConfigurationOptions options)
    {
        var proxies = options.KnownProxies.Select(IPAddress.Parse).ToArray();
        var networks = options.KnownNetworks.Select(x => System.Net.IPNetwork.Parse(x)).ToArray();

        return services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

            foreach (var proxy in proxies)
            {
                o.KnownProxies.Add(proxy);
            }

            foreach (var network in networks)
            {
                o.KnownIPNetworks.Add(network);
            }
        });
    }

    internal static IServiceCollection ConfigureAuroreKestrelLimits(this IServiceCollection services, KestrelLimitsOptions options)
    {
        return services.Configure<KestrelServerOptions>(o =>
        {
            o.Limits.MaxConcurrentConnections = options.MaxConcurrentConnections;
            o.Limits.MaxRequestHeaderCount = options.MaxRequestHeaderCount;
            o.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(options.RequestHeadersTimeoutSeconds);
            o.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(options.KeepAliveTimeoutSeconds);
            o.Limits.MaxRequestBodySize = options.MaxRequestBodySizeBytes;
        });
    }

    /// <summary>
    /// Timeouts only cancel HttpContext.RequestAborted: endpoints must pass that token to EF Core, HttpClient, etc.
    /// (declare a CancellationToken handler parameter; analyzer CA2016 enforces forwarding it).
    /// Timeouts are not enforced while a debugger is attached.
    /// </summary>
    internal static IServiceCollection AddAuroreRequestTimeouts(this IServiceCollection services, RequestTimeoutsConfigurationOptions options)
    {
        return services.AddRequestTimeouts(o =>
        {
            o.DefaultPolicy = new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(options.DefaultSeconds) };

            foreach (var (name, seconds) in options.Policies)
            {
                o.AddPolicy(name, TimeSpan.FromSeconds(seconds));
            }
        });
    }

    /// <summary>
    /// Registers named policies only; there is no global limiter. Endpoints (or whole groups) opt in with .RequireRateLimiting("&lt;name&gt;").
    /// Each caller gets its own window: the authenticated user id, or the client IP for anonymous requests.
    /// Limits are per instance: with N instances the effective limit is N times higher.
    /// </summary>
    internal static IServiceCollection AddAuroreRateLimiting(this IServiceCollection services, RateLimitingConfigurationOptions options)
    {
        return services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = WriteRateLimitedResponseAsync;

            foreach (var (name, policy) in options.Policies)
            {
                o.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
                    GetRateLimitPartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = policy.PermitLimit,
                        Window = TimeSpan.FromSeconds(policy.WindowSeconds),
                        QueueLimit = 0,
                    }));
            }
        });
    }

    /// <summary>
    /// Registers named policies only; endpoints opt in with .CacheOutput("&lt;name&gt;"). The cache is shared by all callers
    /// unless the policy varies by a header such as Authorization.
    /// </summary>
    internal static IServiceCollection AddAuroreOutputCache(this IServiceCollection services, OutputCacheConfigurationOptions options)
    {
        return services.AddOutputCache(o =>
        {
            foreach (var (name, policy) in options.Policies)
            {
                o.AddPolicy(name, builder =>
                {
                    builder
                        .Cache()
                        .Expire(TimeSpan.FromSeconds(policy.ExpirationSeconds))
                        .SetVaryByQuery(policy.VaryByQuery);

                    if (policy.VaryByHeader.Length > 0)
                    {
                        builder.SetVaryByHeader(policy.VaryByHeader);
                    }
                });
            }
        });
    }

    /// <summary>
    /// HybridCache uses memory only for now. When Redis is added (services.AddStackExchangeRedisCache(...)), HybridCache
    /// picks the registered IDistributedCache up as its second level automatically; nothing else changes.
    /// </summary>
    internal static IServiceCollection AddAuroreIdempotency(this IServiceCollection services, IdempotencyOptions options)
    {
        services.AddHybridCache();

        return services.AddSingleton(options);
    }

    internal static IServiceCollection AddAuroreExceptionHandler(this IServiceCollection services)
    {
        return services
            .AddExceptionHandler<UnexpectedExceptionHandler>()
            .AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions.Remove("traceId");
            });
    }

    internal static IApplicationBuilder UseAuroreSecurityHeaders(this IApplicationBuilder builder, SecurityHeadersOptions options)
    {
        var isProduction = builder.ApplicationServices.GetRequiredService<IHostEnvironment>().IsProduction();

        if (isProduction)
        {
            builder.UseHsts();
        }

        var effectiveOptions = isProduction ? options : options with { ContentSecurityPolicy = null };

        return builder.UseMiddleware<SecurityHeadersMiddleware>(effectiveOptions);
    }

    private static async ValueTask WriteRateLimitedResponseAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        await TypedResults
            .Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "Too Many Requests", detail: "Too many requests. Retry later.")
            .ExecuteAsync(context.HttpContext);
    }

    private static string GetRateLimitPartitionKey(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
    }
}
