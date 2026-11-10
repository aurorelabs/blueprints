using Aurore.Foundation.Telemetry.Models;
using Aurore.Foundation.Telemetry.Validators;

namespace Tests.Telemetry.Validators;

[Trait("Feature", "Telemetry validation")]
public sealed class TelemetryOptionsValidatorTests
{
    public static TheoryData<string, string, string> InvalidEndpointOrProtocol => new()
    {
        { "not a url", "Grpc", "Otlp.Endpoint" },
        { "http://collector:4317", "Carrier pigeon", "Otlp.Protocol" },
    };

    public static TheoryData<int, int, int, double, string> OutOfRangeValues => new()
    {
        { 0, 60, 30, 1.0, "Otlp.RequestTimeoutSeconds" },
        { 10, 0, 30, 1.0, "Metrics.ExportIntervalSeconds" },
        { 10, 60, 0, 1.0, "Metrics.ExportTimeoutSeconds" },
        { 10, 60, 30, 1.5, "Tracing.SamplingRatio" },
    };

    public static TheoryData<int, int, int, int, string> InvalidBatchExport => new()
    {
        { 0, 512, 5, 30, "MaxQueueSize" },
        { 2048, 0, 5, 30, "MaxExportBatchSize" },
        { 2048, 4096, 5, 30, "MaxExportBatchSize" },
        { 2048, 512, 0, 30, "ScheduledDelaySeconds" },
        { 2048, 512, 5, 0, "ExportTimeoutSeconds" },
    };

    [Fact(DisplayName = "Default options with a collector endpoint are valid")]
    public void DefaultOptionsAreValid()
    {
        // Arrange
        var validator = new TelemetryOptionsValidator(isProduction: true);

        // Act
        var result = validator.Validate(Options());

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "A local collector is accepted in development")]
    public void LocalCollectorIsAcceptedInDevelopment()
    {
        // Arrange
        var options = Options(endpoint: "http://localhost:4317");
        var validator = new TelemetryOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "A local collector is rejected in production")]
    public void LocalCollectorIsRejectedInProduction()
    {
        // Arrange
        var options = Options(endpoint: "http://localhost:4317");
        var validator = new TelemetryOptionsValidator(isProduction: true);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "Otlp.Endpoint" && x.ErrorMessage.Contains("local address"));
    }

    [Theory(DisplayName = "Rejects a collector endpoint that is not an absolute URL or a protocol that is not an OTLP protocol")]
    [MemberData(nameof(InvalidEndpointOrProtocol))]
    public void RejectsInvalidEndpointOrProtocol(string endpoint, string protocol, string expectedProperty)
    {
        // Arrange
        var validator = new TelemetryOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(Options(endpoint, protocol));

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == expectedProperty);
    }

    [Theory(DisplayName = "Rejects timeouts, intervals and sampling ratios that are out of range")]
    [MemberData(nameof(OutOfRangeValues))]
    public void RejectsOutOfRangeValues(int requestTimeout, int interval, int exportTimeout, double ratio, string expectedProperty)
    {
        // Arrange
        var options = Options() with
        {
            Otlp = Options().Otlp with { RequestTimeoutSeconds = requestTimeout },
            Metrics = new() { ExportIntervalSeconds = interval, ExportTimeoutSeconds = exportTimeout },
            Tracing = new() { SamplingRatio = ratio },
        };
        var validator = new TelemetryOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == expectedProperty);
    }

    [Theory(DisplayName = "Rejects batch export options that cannot work")]
    [MemberData(nameof(InvalidBatchExport))]
    public void RejectsInvalidBatchExportOptions(int queue, int batch, int delay, int timeout, string expectedProperty)
    {
        // Arrange
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
        var validator = new TelemetryOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == $"Logging.BatchExport.{expectedProperty}");
    }

    private static TelemetryOptions Options(string endpoint = "http://collector:4317", string protocol = "Grpc")
    {
        return new TelemetryOptions { Otlp = new TelemetryOtlpOptions { Endpoint = endpoint, Protocol = protocol } };
    }
}
