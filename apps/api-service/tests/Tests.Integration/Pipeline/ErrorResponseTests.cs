using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Integration.Pipeline;

[Trait("Feature", "Error responses")]
public sealed class ErrorResponseTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(DisplayName = "An unhandled exception returns a 500 problem that still carries the caller's correlation id")]
    public async Task UnhandledExceptionKeepsTheCorrelationId()
    {
        // Arrange
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/demo/diagnostics/error");
        request.Headers.Add("Correlation-Id", "my-correlation-id");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("my-correlation-id", response.Headers.GetValues("Correlation-Id").Single());
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("Test exception", body);
    }

    [Fact(DisplayName = "An unknown route returns a 404 with a problem body instead of an empty response")]
    public async Task UnknownRouteReturnsAProblem()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync("/v1/nothing-here", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact(DisplayName = "A protected endpoint called without credentials returns a 401 with a problem body")]
    public async Task UnauthenticatedCallReturnsAProblem()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        using var response = await client.GetAsync("/v1/demo/auth/authenticated", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }
}
