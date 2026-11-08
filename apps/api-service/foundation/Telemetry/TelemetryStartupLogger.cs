using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Telemetry.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.Telemetry;

/// <summary>
/// OTLP export failures are silent (the SDK only reports them through its own diagnostics), so the effective settings are
/// logged once at startup to make a wrong endpoint or protocol easy to spot.
/// </summary>
internal sealed class TelemetryStartupLogger(ILogger<TelemetryStartupLogger> logger, TelemetryOptions telemetry) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Telemetry is exported with {Protocol} to {Endpoint}; traces sampled at {SamplingRatio:P0}",
            telemetry.Otlp.Protocol,
            telemetry.Otlp.Endpoint,
            telemetry.Tracing.SamplingRatio);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
