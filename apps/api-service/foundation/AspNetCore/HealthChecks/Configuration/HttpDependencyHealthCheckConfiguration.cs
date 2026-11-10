using System;
using System.Linq;
using Aurore.Foundation.AspNetCore.HealthChecks.Checks;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.HealthChecks.Constants;
using Aurore.Foundation.Core.HttpDependencies.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Configuration;

/// <summary>Registers the health check of an upstream HTTP service.</summary>
public static class HttpDependencyHealthCheckConfiguration
{
    /// <summary>
    /// Adds a check that calls the dependency's readiness endpoint. Critical dependencies make the service unhealthy when
    /// they fail, others only degraded.
    /// </summary>
    public static IHealthChecksBuilder AddAuroreHttpDependencyCheck(
        this IHealthChecksBuilder builder,
        AppInformationOptions appInfo,
        string name,
        string baseUrl,
        HttpDependencyHealthCheckOptions options)
    {
        var checkName = name.ToSlugCase();

        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out _) is false)
            throw new InvalidOperationException($"Dependency '{checkName}' has an invalid BaseUrl '{baseUrl}'. It must be an absolute URL, e.g. 'https://orders.internal'.");

        if (options.TimeoutSeconds <= 0)
            throw new InvalidOperationException($"Dependency '{checkName}' has an invalid health check TimeoutSeconds '{options.TimeoutSeconds}'. It must be greater than zero.");

        var timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        var failureStatus = options.Critical ? HealthStatus.Unhealthy : HealthStatus.Degraded;

        var tags = new[]
            {
                HealthCheckProperties.Tags.Readiness,
                HealthCheckProperties.Tags.TypeHttpService,
                options.Critical ? HealthCheckProperties.Tags.Critical : HealthCheckProperties.Tags.NonCritical,
            }
            .Concat(options.Tags.Select(x => x.ToSlugCase()))
            .Distinct(StringComparer.Ordinal);

        builder.Services.AddHttpClient(HttpDependencyHealthCheck.GetClientName(checkName), client =>
        {
            client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/{options.ReadinessEndpoint.TrimStart('/')}");
            client.Timeout = timeout;
            client.DefaultRequestHeaders.Add(HealthCheckProperties.Headers.Client, appInfo.Component.ToSlugCase());
        });

        return builder.AddCheck<HttpDependencyHealthCheck>(checkName, failureStatus, tags, timeout);
    }
}
