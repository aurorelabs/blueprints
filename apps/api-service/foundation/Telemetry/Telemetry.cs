using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Aurore.Foundation.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Aurore.Foundation.Telemetry;

public record TelemetryOptions
{
    public required string CollectorEndpoint { get; init; }
    public required string Protocol { get; init; }
    public string Headers { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, string> ResourceAttributes { get; init; } = new Dictionary<string, string>();
    public TelemetryLoggingOptions Logging { get; init; } = new();
    public TelemetryMetricsOptions Metrics { get; init; } = new();
    public TelemetryTracingOptions Tracing { get; init; } = new();
}

public record TelemetryLoggingOptions
{
    public bool IncludeScopes { get; init; } = true;
    public bool ParseStateValues { get; init; } = true;
    public bool ExportToConsole { get; init; } = false;
}

public record TelemetryMetricsOptions
{
    public bool ExportToConsole { get; init; } = false;
}

public record TelemetryTracingOptions
{
    public double TraceIdRatioBasedSampler { get; init; } = 1.0;
    public string[] IncomingTrafficFilters { get; init; } = [];
    public string[] OutgoingTrafficFilters { get; init; } = [];
    public bool ExportToConsole { get; init; } = false;
}

public static class OpenTelemetryProperties
{
    public static class Sources
    {
        public const string Infrastructure = "Aurore.Platform.Infrastructure";
    }

    public static class Attributes
    {
        public static class Standard
        {
            public const string EnvironmentName = "deployment.environment.name";
            public const string ServiceInstanceId = "service.instance.id";
            public const string ServiceName = "service.name";
            public const string ServiceVersion = "service.version";
            public const string HttpRequestMethod = "http.request.method";
            public const string ServerAddress = "server.address";
            public const string UrlPath = "url.path";
            public const string UrlQuery = "url.query";
            public const string ClientAddress = "client.address";
            public const string UserAgentOriginal = "user_agent.original";
        }

        public static class Custom
        {
            private const string Prefix = "aur";

            public const string ServiceDomain = $"{Prefix}.service.domain";
            public const string ServiceBoundary = $"{Prefix}.service.boundary";
            public const string ServiceComponent = $"{Prefix}.service.component";
            public const string ServiceMaintainer = $"{Prefix}.service.maintainer";
            public const string CorrelationId = $"{Prefix}.correlation.id";
            public const string MessageHandler = $"{Prefix}.messaging.handler";
            public const string TraceId = $"{Prefix}.trace.id";
            public const string SpanId = $"{Prefix}.span.id";
        }
    }
}

public static class TelemetryConfiguration
{
    private const string PostgresSourceName = "Npgsql";

    public static IHostApplicationBuilder AddAuroreTelemetry(this IHostApplicationBuilder builder, AppInformationOptions appInfo, TelemetryOptions telemetry)
    {
        builder.Services
            .AddLogging(x => x.ClearProviders())
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
                logging =>
                {
                    logging
                        .AddOtlpExporter(ConfigureOtlpExporter(telemetry));

                    if (telemetry.Logging.ExportToConsole)
                        logging.AddConsoleExporter();
                },
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
            x.Protocol = telemetry.Protocol.ParseToEnum<OtlpExportProtocol>();
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
