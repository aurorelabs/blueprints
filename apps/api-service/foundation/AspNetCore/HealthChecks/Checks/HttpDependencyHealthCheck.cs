using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Checks;

public sealed class HttpDependencyHealthCheck(ILogger<HttpDependencyHealthCheck> logger, IHttpClientFactory clientFactory) : IHealthCheck
{
    private const string ClientNamePrefix = "health-check-";

    public static string GetClientName(string checkName)
    {
        return $"{ClientNamePrefix}{checkName}";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var name = context.Registration.Name;
        var failureStatus = context.Registration.FailureStatus;

        try
        {
            var client = clientFactory.CreateClient(GetClientName(name));

            using var response = await client.GetAsync(string.Empty, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy();

            logger.LogWarning("Health check for dependency {DependencyName} returned status code {StatusCode}.", name, (int)response.StatusCode);

            return new HealthCheckResult(failureStatus, $"Dependency returned status code {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check for dependency {DependencyName} could not reach the dependency.", name);

            return new HealthCheckResult(failureStatus, "Dependency could not be reached.", ex);
        }
    }
}
