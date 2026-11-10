using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Integration.Pipeline;

[Trait("Feature", "Request context")]
public sealed class RequestContextTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CorrelationEndpoint = "/v1/demo/diagnostics/correlation";

    [Fact(DisplayName = "The Correlation-Id the caller sends is used and echoed back on the response")]
    public async Task CorrelationIdIsEchoed()
    {
        // Arrange
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, CorrelationEndpoint);
        request.Headers.Add("Correlation-Id", "my-correlation-id");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("my-correlation-id", response.Headers.GetValues("Correlation-Id").Single());
    }

    [Fact(DisplayName = "Without a header the correlation id is the trace id, and the response says so")]
    public async Task CorrelationIdDefaultsToTheTraceId()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync(CorrelationEndpoint, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        var correlationId = response.Headers.GetValues("Correlation-Id").Single();
        Assert.Equal(32, correlationId.Length);
        Assert.Contains($"\"traceId\":\"{correlationId}\"", body);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }
}
