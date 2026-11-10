using System;
using System.Collections.Generic;
using System.Linq;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Constants;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.Privacy;
using Aurore.Foundation.Telemetry.Models;
using Aurore.Foundation.Telemetry.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Aurore.Foundation.Telemetry.Configuration;

/// <summary>Registers OpenTelemetry for the application: logs, metrics and traces exported over OTLP.</summary>
public static class TelemetryConfiguration
{
    private const string PostgresSourceName = "Npgsql";
    private const string OtlpTraceSignal = "traces";
    private const string OtlpMetricSignal = "metrics";
    private const string OtlpLogSignal = "logs";

    /// <summary>
    /// Registers OpenTelemetry logs, metrics and traces exported over OTLP, plus a console logger (single lines in
    /// Development, JSON elsewhere) so logs stay visible when the collector is unreachable.
    /// </summary>
    public static IHostApplicationBuilder AddAuroreTelemetry(this IHostApplicationBuilder builder, AppInformationOptions appInfo, TelemetryOptions telemetry)
    {
        builder.Logging.ClearProviders();

        AddConsoleLogger(builder);

        builder.Services.AddHostedService(provider => new TelemetryStartupLogger(provider.GetRequiredService<ILogger<TelemetryStartupLogger>>(), telemetry));

        builder.Services
            .AddOpenTelemetry()
            .ConfigureAppResource(builder.Environment, appInfo, telemetry)
            .ConfigureLogging(telemetry)
            .ConfigureMetrics(telemetry)
            .ConfigureTracing(appInfo, telemetry);

        return builder;

        static void AddConsoleLogger(IHostApplicationBuilder builder)
        {
            if (builder.Environment.IsDevelopment())
            {
                builder.Logging.AddSimpleConsole(o =>
                {
                    o.SingleLine = true;
                    o.TimestampFormat = TimestampFormats.TimeOnlyWithTrailingSpace;
                });
            }
            else
            {
                builder.Logging.AddJsonConsole(o =>
                {
                    o.IncludeScopes = true;
                    o.UseUtcTimestamp = true;
                    o.TimestampFormat = TimestampFormats.Iso8601.UtcWithMilliseconds;
                });
            }
        }
    }

    private static OpenTelemetryBuilder ConfigureAppResource(this OpenTelemetryBuilder builder, IHostEnvironment env, AppInformationOptions appInfo, TelemetryOptions telemetry)
    {
        return builder
            .ConfigureResource(resource => resource
                .AddService(
                    appInfo.Component.ToSlugCase(),
                    appInfo.Namespace.ToSlugCase(),
                    appInfo.Version,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes(telemetry.ResourceAttributes.Select(x => new KeyValuePair<string, object>(x.Key, x.Value)))
                .AddAttributes([
                    new(OpenTelemetryProperties.Attributes.Standard.EnvironmentName, env.EnvironmentName),
                    new(OpenTelemetryProperties.Attributes.Custom.ServiceDomain, appInfo.Domain.ToSlugCase()),
                    new(OpenTelemetryProperties.Attributes.Custom.ServiceBoundary, appInfo.Boundary.ToSlugCase()),
                    new(OpenTelemetryProperties.Attributes.Custom.ServiceMaintainer, appInfo.Maintainer.Name.ToSlugCase()),
                ]));
    }

    private static OpenTelemetryBuilder ConfigureLogging(this OpenTelemetryBuilder builder, TelemetryOptions telemetry)
    {
        return builder
            .WithLogging(
                logging => logging.AddOtlpExporter((exporter, processor) =>
                {
                    ConfigureOtlpExporter(telemetry, OtlpLogSignal)(exporter);

                    var batch = telemetry.Logging.BatchExport;
                    processor.BatchExportProcessorOptions.MaxQueueSize = batch.MaxQueueSize;
                    processor.BatchExportProcessorOptions.MaxExportBatchSize = batch.MaxExportBatchSize;
                    processor.BatchExportProcessorOptions.ScheduledDelayMilliseconds = ToMilliseconds(batch.ScheduledDelaySeconds);
                    processor.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = ToMilliseconds(batch.ExportTimeoutSeconds);
                }),
                options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = telemetry.Logging.IncludeScopes;
                    options.ParseStateValues = telemetry.Logging.ParseStateValues;
                });
    }

    private static OpenTelemetryBuilder ConfigureMetrics(this OpenTelemetryBuilder builder, TelemetryOptions telemetry)
    {
        return builder
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter(PostgresSourceName)
                    // Custom meters are registered here: see docs/telemetry.md ("add a custom metric").
                    .AddOtlpExporter((exporter, reader) =>
                    {
                        ConfigureOtlpExporter(telemetry, OtlpMetricSignal)(exporter);

                        reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = ToMilliseconds(telemetry.Metrics.ExportIntervalSeconds);
                        reader.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = ToMilliseconds(telemetry.Metrics.ExportTimeoutSeconds);
                    });

                if (telemetry.Metrics.ExportToConsole)
                    metrics.AddConsoleExporter();
            });
    }

    private static OpenTelemetryBuilder ConfigureTracing(this OpenTelemetryBuilder builder, AppInformationOptions appInfo, TelemetryOptions telemetry)
    {
        var sampler = new ParentBasedSampler(new TraceIdRatioBasedSampler(telemetry.Tracing.SamplingRatio));

        return builder
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(appInfo.ActivitySource)
                    .AddSource(OpenTelemetryProperties.Sources.Infrastructure)
                    .AddSource(PostgresSourceName)
                    // Additional ActivitySources are registered here: see docs/telemetry.md ("add a trace source").
                    .SetSampler(sampler)
                    .AddIncomingTrafficInstrumentation(telemetry.Tracing.ExcludedIncomingPaths)
                    .AddOutgoingTrafficInstrumentation(telemetry.Tracing.ExcludedOutgoingPaths)
                    .SetErrorStatusOnException()
                    // Must come before the exporters: it encrypts personal data in tags before they are exported.
                    .AddProcessor(provider => new PrivacyActivityProcessor(provider.GetService<IPrivacyProtector>() ?? new PrivacyProtector(new())))
                    .AddOtlpExporter(exporter =>
                    {
                        ConfigureOtlpExporter(telemetry, OtlpTraceSignal)(exporter);

                        var batch = telemetry.Tracing.BatchExport;
                        exporter.BatchExportProcessorOptions.MaxQueueSize = batch.MaxQueueSize;
                        exporter.BatchExportProcessorOptions.MaxExportBatchSize = batch.MaxExportBatchSize;
                        exporter.BatchExportProcessorOptions.ScheduledDelayMilliseconds = ToMilliseconds(batch.ScheduledDelaySeconds);
                        exporter.BatchExportProcessorOptions.ExporterTimeoutMilliseconds = ToMilliseconds(batch.ExportTimeoutSeconds);
                    });

                if (telemetry.Tracing.ExportToConsole)
                    tracing.AddConsoleExporter();
            });
    }

    private static Action<OtlpExporterOptions> ConfigureOtlpExporter(TelemetryOptions telemetry, string signal)
    {
        return x =>
        {
            var otlp = telemetry.Otlp;
            var protocol = otlp.Protocol.ParseEnum<OtlpExportProtocol>();

            x.Endpoint = BuildOtlpEndpoint(otlp.Endpoint, protocol, signal);
            x.Protocol = protocol;
            x.Headers = otlp.Headers;
            x.TimeoutMilliseconds = ToMilliseconds(otlp.RequestTimeoutSeconds);
        };
    }

    /// <summary>
    /// Settings are in seconds; the OpenTelemetry SDK takes milliseconds.
    /// </summary>
    private static int ToMilliseconds(int seconds)
    {
        return (int)TimeSpan.FromSeconds(seconds).TotalMilliseconds;
    }

    /// <summary>
    /// gRPC uses the collector endpoint as-is. HTTP/protobuf needs the per-signal path (/v1/traces, /v1/metrics,
    /// /v1/logs) because the SDK does not append it when the endpoint is set in code.
    /// </summary>
    internal static Uri BuildOtlpEndpoint(string collectorEndpoint, OtlpExportProtocol protocol, string signal)
    {
        return protocol == OtlpExportProtocol.HttpProtobuf
            ? new Uri($"{collectorEndpoint.TrimEnd('/')}/v1/{signal}")
            : new Uri(collectorEndpoint);
    }

    private static TracerProviderBuilder AddOutgoingTrafficInstrumentation(
        this TracerProviderBuilder builder, IEnumerable<string> configuredExcludedPaths)
    {
        var excludedPaths = WithHealthChecks(configuredExcludedPaths);

        return builder
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.FilterHttpRequestMessage = request =>
                {
                    if (request.RequestUri is null)
                        return false;

                    var path = request.RequestUri.PathAndQuery;

                    return !excludedPaths.Any(x => path.StartsWith(x, StringComparison.OrdinalIgnoreCase));
                };
            });
    }

    private static TracerProviderBuilder AddIncomingTrafficInstrumentation(
        this TracerProviderBuilder builder, IEnumerable<string> configuredExcludedPaths)
    {
        var excludedPaths = WithHealthChecks(configuredExcludedPaths);

        return builder
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = context =>
                {
                    var path = context.Request.Path.Value ?? string.Empty;

                    return !excludedPaths.Any(x => path.StartsWith(x, StringComparison.OrdinalIgnoreCase));
                };
            });
    }

    /// <summary>
    /// Health probes are frequent and uninteresting, so they are never traced, whatever is configured.
    /// </summary>
    private static string[] WithHealthChecks(IEnumerable<string> excludedPaths)
    {
        return [.. excludedPaths.Concat(["/health/"])];
    }
}
