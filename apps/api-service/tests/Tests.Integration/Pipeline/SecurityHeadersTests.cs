using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Integration.Pipeline;

[Trait("Feature", "Security headers")]
public sealed class SecurityHeadersTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(DisplayName = "Every response carries the security headers, and the content security policy waits for production so the API documentation can load")]
    public async Task ResponsesCarryTheSecurityHeaders()
    {
        // Arrange
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        // Act
        using var response = await client.GetAsync("/v1/demo/diagnostics/correlation", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.False(response.Headers.Contains("Content-Security-Policy"));
    }
}
