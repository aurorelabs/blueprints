using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Integration.OpenApi;

[Trait("Feature", "OpenAPI documents")]
public sealed class OpenApiSecurityTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory(DisplayName = "An operation lists security schemes only when it requires authorization")]
    [InlineData("/v1/examples/authenticated", true)] // the group requires authorization
    [InlineData("/v1/tests/authorized", true)] // the group is anonymous, the endpoint requires authorization
    [InlineData("/v1/tests/error", false)] // anonymous group and endpoint
    public async Task OperationSecurityFollowsTheAuthorizationMetadata(string path, bool expectedSecured)
    {
        using var document = await GetDocumentAsync("v1");

        var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("get");

        var secured = operation.TryGetProperty("security", out var security) && security.GetArrayLength() > 0;

        Assert.Equal(expectedSecured, secured);
    }

    [Fact(DisplayName = "The document does not apply security to every operation")]
    public async Task DocumentHasNoGlobalSecurity()
    {
        using var document = await GetDocumentAsync("v1");

        Assert.False(document.RootElement.TryGetProperty("security", out _));
    }

    [Fact(DisplayName = "Each API version has its own document, and unknown versions have none")]
    public async Task EachVersionHasItsOwnDocument()
    {
        using var client = CreateClient();

        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/openapi/v2.json", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v3.json", TestContext.Current.CancellationToken)).StatusCode);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    private async Task<JsonDocument> GetDocumentAsync(string version)
    {
        using var client = CreateClient();

        var json = await client.GetStringAsync($"/openapi/{version}.json", TestContext.Current.CancellationToken);

        return JsonDocument.Parse(json);
    }
}
