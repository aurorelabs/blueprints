using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Integration.OpenApi;

[Trait("Feature", "OpenAPI documents")]
public sealed class ScalarAuthenticationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(DisplayName = "The Scalar page is preconfigured for OAuth2 sign-in with PKCE")]
    public async Task ScalarPageCarriesTheOAuth2Configuration()
    {
        // Arrange
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        // Act
        var html = await client.GetStringAsync("/scalar/v1", TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("api-docs", html);
        Assert.Contains("OAuth2", html);
        Assert.Contains("SHA-256", html);
    }
}
