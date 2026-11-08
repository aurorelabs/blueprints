using Aurore.Foundation.Telemetry.Configuration;
using OpenTelemetry.Exporter;

namespace Tests.Telemetry.Configuration;

[Trait("Feature", "Telemetry")]
public sealed class TelemetryConfigurationTests
{
    [Theory(DisplayName = "Otlp endpoint only gets a signal path over HTTP")]
    [InlineData("http://collector:4317", OtlpExportProtocol.Grpc, "traces", "http://collector:4317/")]
    [InlineData("http://collector:4318", OtlpExportProtocol.HttpProtobuf, "traces", "http://collector:4318/v1/traces")]
    [InlineData("http://collector:4318/", OtlpExportProtocol.HttpProtobuf, "metrics", "http://collector:4318/v1/metrics")]
    [InlineData("http://collector:4318", OtlpExportProtocol.HttpProtobuf, "logs", "http://collector:4318/v1/logs")]
    public void OtlpEndpointOnlyGetsASignalPathOverHttp(string collector, OtlpExportProtocol protocol, string signal, string expected)
    {
        var endpoint = TelemetryConfiguration.BuildOtlpEndpoint(collector, protocol, signal);

        Assert.Equal(expected, endpoint.ToString());
    }
}
