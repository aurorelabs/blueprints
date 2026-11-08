using Aurore.Foundation.Telemetry.Models;
using Aurore.Foundation.Telemetry.Validators;

namespace Tests.Telemetry.Validators;

[Trait("Feature", "Telemetry validation")]
public sealed class TelemetryOptionsValidatorTests
{
    [Fact(DisplayName = "Default options with a collector endpoint are valid")]
    public void DefaultOptionsAreValid()
    {
        var result = new TelemetryOptionsValidator(isProduction: true).Validate(Options());

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "A local collector is accepted in development but rejected in production")]
    public void LocalCollectorIsOnlyAcceptedInDevelopment()
    {
        var options = Options(endpoint: "http://localhost:4317");

        Assert.True(new TelemetryOptionsValidator(isProduction: false).Validate(options).IsValid);

        var result = new TelemetryOptionsValidator(isProduction: true).Validate(options);

        Assert.Contains(result.Errors, x => x.PropertyName == "Otlp.Endpoint" && x.ErrorMessage.Contains("local address"));
    }

    [Theory(DisplayName = "Rejects a collector endpoint that is not an absolute URL or a protocol that is not an OTLP protocol")]
    [InlineData("not a url", "Grpc", "Otlp.Endpoint")]
    [InlineData("http://collector:4317", "Carrier pigeon", "Otlp.Protocol")]
    public void RejectsInvalidEndpointOrProtocol(string endpoint, string protocol, string expectedProperty)
    {
        var result = new TelemetryOptionsValidator(isProduction: false).Validate(Options(endpoint, protocol));

        Assert.Contains(result.Errors, x => x.PropertyName == expectedProperty);
    }

    [Theory(DisplayName = "Rejects timeouts, intervals and sampling ratios that are out of range")]
    [InlineData(0, 60, 30, 1.0, "Otlp.RequestTimeoutSeconds")]
    [InlineData(10, 0, 30, 1.0, "Metrics.ExportIntervalSeconds")]
    [InlineData(10, 60, 0, 1.0, "Metrics.ExportTimeoutSeconds")]
    [InlineData(10, 60, 30, 1.5, "Tracing.SamplingRatio")]
    public void RejectsOutOfRangeValues(int requestTimeout, int interval, int exportTimeout, double ratio, string expectedProperty)
    {
        var options = Options() with
        {
            Otlp = Options().Otlp with { RequestTimeoutSeconds = requestTimeout },
            Metrics = new() { ExportIntervalSeconds = interval, ExportTimeoutSeconds = exportTimeout },
            Tracing = new() { SamplingRatio = ratio },
        };

        var result = new TelemetryOptionsValidator(isProduction: false).Validate(options);

        Assert.Contains(result.Errors, x => x.PropertyName == expectedProperty);
    }

    [Theory(DisplayName = "Rejects batch export options that cannot work")]
    [InlineData(0, 512, 5, 30, "MaxQueueSize")]
    [InlineData(2048, 0, 5, 30, "MaxExportBatchSize")]
    [InlineData(2048, 4096, 5, 30, "MaxExportBatchSize")]
    [InlineData(2048, 512, 0, 30, "ScheduledDelaySeconds")]
    [InlineData(2048, 512, 5, 0, "ExportTimeoutSeconds")]
    public void RejectsInvalidBatchExportOptions(int queue, int batch, int delay, int timeout, string expectedProperty)
    {
        var options = Options() with
        {
            Logging = new()
            {
                BatchExport = new()
                {
                    MaxQueueSize = queue,
                    MaxExportBatchSize = batch,
                    ScheduledDelaySeconds = delay,
                    ExportTimeoutSeconds = timeout,
                },
            },
        };

        var result = new TelemetryOptionsValidator(isProduction: false).Validate(options);

        Assert.Contains(result.Errors, x => x.PropertyName == $"Logging.BatchExport.{expectedProperty}");
    }

    private static TelemetryOptions Options(string endpoint = "http://collector:4317", string protocol = "Grpc")
    {
        return new TelemetryOptions { Otlp = new TelemetryOtlpOptions { Endpoint = endpoint, Protocol = protocol } };
    }
}
