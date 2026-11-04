using System;
using System.Collections.Generic;
using Aurore.Foundation.AspNetCore.HealthChecks.Models;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.HealthChecks.Constants;
using Aurore.Foundation.Core.HealthChecks.Models;
using Aurore.Foundation.Core.HttpDependencies.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Configuration;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddAuroreHealthChecks(
        this IServiceCollection services,
        AppInformationOptions appInfo,
        HealthChecksOptions options,
        IReadOnlyDictionary<string, HttpDependencyOptions> dependencies)
    {
        var healthChecks = services.AddHealthChecks();

        foreach (var (name, dependency) in dependencies)
        {
            if (dependency.HealthCheck is not null)
                healthChecks.AddAuroreHttpDependencyCheck(appInfo, name, dependency.BaseUrl, dependency.HealthCheck);
        }

        services.AddOutputCache(o => o
            .AddPolicy(HealthCheckProperties.Configuration.OutputCacheKey, policy => policy
                .Expire(TimeSpan.FromSeconds(options.CacheSeconds))
                .Cache()));

        return services;
    }

    /// <summary>
    /// /health/live    The process is up. Anonymous and never touches dependencies, so it is safe for frequent probes.
    /// /health/ready   Critical dependencies only, status without details. Anonymous, for load balancers and orchestrators.
    /// /health/status  All dependencies with details. Requires an authenticated caller since it reveals dependency names.
    ///
    /// HTTP status codes use the framework defaults: Healthy and Degraded return 200, Unhealthy returns 503.
    /// Non-critical dependencies are registered with a Degraded failure status, so they never make the service Unhealthy.
    /// </summary>
    public static WebApplication MapAuroreHealthChecks(this WebApplication app, AppInformationOptions appInfo)
    {
        var serviceName = appInfo.Component.ToSlugCase();

        app.MapGet(HealthCheckProperties.Endpoints.Liveness, () => Results.Ok(LivenessReport.Create(serviceName, appInfo.Version)))
            .WithName("HealthCheckLiveness")
            .ExcludeFromDescription();

        app.MapHealthChecks(HealthCheckProperties.Endpoints.Readiness, CreateOptions(IsCriticalReadinessCheck, includeDependencies: false))
            .WithName("HealthCheckReadiness")
            .CacheOutput(HealthCheckProperties.Configuration.OutputCacheKey);

        app.MapHealthChecks(HealthCheckProperties.Endpoints.Status, CreateOptions(IsReadinessCheck, includeDependencies: true))
            .WithName("HealthCheckStatus")
            .CacheOutput(HealthCheckProperties.Configuration.OutputCacheKey)
            .RequireAuthorization();

        return app;
    }

    private static HealthCheckOptions CreateOptions(Func<HealthCheckRegistration, bool> predicate, bool includeDependencies)
    {
        return new HealthCheckOptions
        {
            Predicate = predicate,
            AllowCachingResponses = true,
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(
                HealthCheckReport.From(report, includeDependencies),
                context.RequestAborted),
        };
    }

    private static bool IsReadinessCheck(HealthCheckRegistration check)
    {
        return check.Tags.Contains(HealthCheckProperties.Tags.Readiness);
    }

    private static bool IsCriticalReadinessCheck(HealthCheckRegistration check)
    {
        return IsReadinessCheck(check) && check.Tags.Contains(HealthCheckProperties.Tags.Critical);
    }
}
