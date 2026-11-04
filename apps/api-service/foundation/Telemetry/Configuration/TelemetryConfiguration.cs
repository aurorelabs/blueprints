using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Telemetry.Constants;
using Aurore.Foundation.Telemetry.Models;
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

public static class TelemetryConfiguration
{
    private const string PostgresSourceName = "Npgsql";

    /// <summary>
    /// Registers OpenTelemetry logs, metrics and traces exported over OTLP, plus a console logger (single lines in
    /// Development, JSON elsewhere) so logs stay visible when the collector is unreachable.
    /// </summary>
    public static IHostApplicationBuilder AddAuroreTelemetry(this IHostApplicationBuilder builder, AppInformationOptions appInfo, TelemetryOptions telemetry)
    {
        builder.Logging.ClearProviders();

        if (builder.Environment.IsDevelopment())
        {
            builder.Logging.AddSimpleConsole(o =>
            {
                o.SingleLine = true;
                o.TimestampFormat = "HH:mm:ss ";
            });
        }
        else
        {
            builder.Logging.AddJsonConsole(o =>
            {
                o.IncludeScopes = true;
                o.UseUtcTimestamp = true;
                o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            });
        }

        builder.Services
            .AddOpenTelemetry()
            .ConfigureAppResource(builder.Environment, appInfo, telemetry)
            .ConfigureLogging(telemetry)
            .ConfigureMetrics(telemetry)
            .ConfigureTracing(appInfo, telemetry);

        return builder;
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
                logging => logging.AddOtlpExporter(ConfigureOtlpExporter(telemetry)),
                options =>
                {
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
                    .AddOtlpExporter(ConfigureOtlpExporter(telemetry));

                if (telemetry.Metrics.ExportToConsole)
                    metrics.AddConsoleExporter();
            });
    }

    private static OpenTelemetryBuilder ConfigureTracing(this OpenTelemetryBuilder builder, AppInformationOptions appInfo, TelemetryOptions telemetry)
    {
        var sampler = new ParentBasedSampler(new TraceIdRatioBasedSampler(telemetry.Tracing.TraceIdRatioBasedSampler));

        return builder
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(appInfo.ActivitySource)
                    .AddSource(OpenTelemetryProperties.Sources.Infrastructure)
                    .AddSource(PostgresSourceName)
                    .SetSampler(sampler)
                    .AddIncomingTrafficInstrumentation(telemetry.Tracing.IncomingTrafficFilters)
                    .AddOutgoingTrafficInstrumentation(telemetry.Tracing.OutgoingTrafficFilters)
                    .SetErrorStatusOnException()
                    .AddOtlpExporter(ConfigureOtlpExporter(telemetry));

                if (telemetry.Tracing.ExportToConsole)
                    tracing.AddConsoleExporter();
            });
    }

    private static Action<OtlpExporterOptions> ConfigureOtlpExporter(TelemetryOptions telemetry)
    {
        return x =>
        {
            x.Endpoint = new Uri(telemetry.CollectorEndpoint);
            x.Protocol = telemetry.Protocol.ParseEnum<OtlpExportProtocol>();
            x.Headers = telemetry.Headers;
        };
    }

    private static TracerProviderBuilder AddOutgoingTrafficInstrumentation(
        this TracerProviderBuilder builder, IEnumerable<string> pathsFilter, Func<HttpRequestMessage, bool>? requestFilter = null)
    {
        var excludedPaths = CreatePathsFilter(pathsFilter);

        return builder
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.FilterHttpRequestMessage = request =>
                {
                    if (request.RequestUri is null)
                        return false;

                    var path = request.RequestUri.PathAndQuery;

                    if (excludedPaths.Any(x => path.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                        return false;

                    return requestFilter is null || requestFilter(request);
                };
            });
    }

    private static TracerProviderBuilder AddIncomingTrafficInstrumentation(
        this TracerProviderBuilder builder, IEnumerable<string> pathsFilter)
    {
        var excludedPaths = CreatePathsFilter(pathsFilter);

        return builder
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = context =>
                {
                    var path = context.Request.Path.Value;

                    if (path is null)
                        return false;

                    return !excludedPaths.Any(x => path.StartsWith(x, StringComparison.OrdinalIgnoreCase));
                };
            });
    }

    private static string[] CreatePathsFilter(IEnumerable<string> paths)
    {
        return [.. paths.Concat(["/health/"])];
    }
}
