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

public static class RequestPipelineConfiguration
{
    public static IServiceCollection ConfigureAuroreForwardedHeaders(this IServiceCollection services, ForwardedHeadersConfigurationOptions options)
    {
        var proxies = options.KnownProxies
            .Select(x => IPAddress.TryParse(x, out var address) ? address : throw InvalidForwardedHeadersValue(x, "KnownProxies"))
            .ToArray();

        var networks = options.KnownNetworks
            .Select(x => System.Net.IPNetwork.TryParse(x, out var network) ? network : throw InvalidForwardedHeadersValue(x, "KnownNetworks"))
            .ToArray();

        return services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;

            foreach (var proxy in proxies)
                o.KnownProxies.Add(proxy);

            foreach (var network in networks)
                o.KnownIPNetworks.Add(network);
        });
    }

    public static IServiceCollection ConfigureAuroreKestrelLimits(this IServiceCollection services, KestrelLimitsOptions options)
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
    /// Timeouts are not enforced while a debugger is attached.
    /// </summary>
    public static IServiceCollection AddAuroreRequestTimeouts(this IServiceCollection services, RequestTimeoutsConfigurationOptions options)
    {
        EnsurePositive(options.DefaultSeconds, "Timeouts:DefaultSeconds");

        foreach (var (name, seconds) in options.Policies)
            EnsurePositive(seconds, $"Timeouts:Policies:{name}");

        return services.AddRequestTimeouts(o =>
        {
            o.DefaultPolicy = new RequestTimeoutPolicy { Timeout = TimeSpan.FromSeconds(options.DefaultSeconds) };

            foreach (var (name, seconds) in options.Policies)
                o.AddPolicy(name, TimeSpan.FromSeconds(seconds));
        });
    }

    /// <summary>
    /// Registers named policies only; there is no global limiter. Endpoints opt in with .RequireRateLimiting("&lt;name&gt;").
    /// Each caller gets its own window: the authenticated user id, or the client IP for anonymous requests.
    /// Limits are per instance: with N instances the effective limit is N times higher.
    /// </summary>
    public static IServiceCollection AddAuroreRateLimiting(this IServiceCollection services, RateLimitingConfigurationOptions options)
    {
        foreach (var (name, policy) in options.Policies)
        {
            EnsurePositive(policy.PermitLimit, $"RateLimiting:Policies:{name}:PermitLimit");
            EnsurePositive(policy.WindowSeconds, $"RateLimiting:Policies:{name}:WindowSeconds");
        }

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
    /// HybridCache uses memory only for now. When Redis is added (services.AddStackExchangeRedisCache(...)), HybridCache
    /// picks the registered IDistributedCache up as its second level automatically; nothing else changes.
    /// </summary>
    public static IServiceCollection AddAuroreIdempotency(this IServiceCollection services, IdempotencyOptions options)
    {
        services.AddHybridCache();

        return services.AddSingleton(options);
    }

    /// <summary>
    /// Endpoints opt in with .WithAuroreIdempotency(); requests carrying an Idempotency-Key header replay the first successful result.
    /// </summary>
    public static TBuilder WithAuroreIdempotency<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter<TBuilder, IdempotencyFilter>();
    }

    /// <summary>
    /// Must run after UseCors(), otherwise browsers see a CORS error instead of the 503 response.
    /// Read once at startup like the rest of AppSettings: changing Enabled requires a restart.
    /// </summary>
    public static IApplicationBuilder UseAuroreMaintenanceMode(this IApplicationBuilder builder, MaintenanceModeOptions options)
    {
        if (options.Enabled is false)
            return builder;

        return builder.UseMiddleware<MaintenanceModeMiddleware>(options);
    }

    public static IServiceCollection AddAuroreExceptionHandler(this IServiceCollection services)
    {
        return services
            .AddExceptionHandler<UnexpectedExceptionHandler>()
            .AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context => context.ProblemDetails.Extensions.Remove("traceId");
            });
    }

    public static IApplicationBuilder UseAuroreSecurityHeaders(this IApplicationBuilder builder, SecurityHeadersOptions options)
    {
        var isProduction = builder.ApplicationServices.GetRequiredService<IHostEnvironment>().IsProduction();

        if (isProduction)
            builder.UseHsts();

        var effectiveOptions = isProduction ? options : options with { ContentSecurityPolicy = null };

        return builder.UseMiddleware<SecurityHeadersMiddleware>(effectiveOptions);
    }

    private static async ValueTask WriteRateLimitedResponseAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

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

    private static void EnsurePositive(int value, string setting)
    {
        if (value <= 0)
            throw new InvalidOperationException($"'{setting}' must be greater than zero, but was {value}.");
    }

    private static InvalidOperationException InvalidForwardedHeadersValue(string value, string setting)
    {
        return new InvalidOperationException($"'{value}' in 'ForwardedHeaders:{setting}' is not a valid value.");
    }
}
