using Aurore.Foundation.Telemetry.Configuration;
using OpenTelemetry.Exporter;

namespace Tests.Telemetry.Configuration;

[Trait("Feature", "Telemetry")]
public sealed class TelemetryConfigurationTests
{
    public static TheoryData<string, OtlpExportProtocol, string, string> OtlpEndpoints => new()
    {
        { "http://collector:4317", OtlpExportProtocol.Grpc, "traces", "http://collector:4317/" },
        { "http://collector:4318", OtlpExportProtocol.HttpProtobuf, "traces", "http://collector:4318/v1/traces" },
        { "http://collector:4318/", OtlpExportProtocol.HttpProtobuf, "metrics", "http://collector:4318/v1/metrics" },
        { "http://collector:4318", OtlpExportProtocol.HttpProtobuf, "logs", "http://collector:4318/v1/logs" },
    };

    [Theory(DisplayName = "The collector endpoint only gets a per-signal path over HTTP")]
    [MemberData(nameof(OtlpEndpoints))]
    public void OtlpEndpointOnlyGetsASignalPathOverHttp(string collector, OtlpExportProtocol protocol, string signal, string expected)
    {
        // Arrange
        // Act
        var endpoint = TelemetryConfiguration.BuildOtlpEndpoint(collector, protocol, signal);

        // Assert
        Assert.Equal(expected, endpoint.ToString());
    }
}
