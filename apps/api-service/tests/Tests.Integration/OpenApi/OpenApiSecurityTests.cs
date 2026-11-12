using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Integration.OpenApi;

[Trait("Feature", "OpenAPI documents")]
public sealed class OpenApiSecurityTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    public static TheoryData<string, bool> OperationSecurity => new()
    {
        { "/v1/demo/auth/authenticated", true }, // the group requires authorization
        { "/v1/demo/diagnostics/authorized", true }, // the group is anonymous, the endpoint requires authorization
        { "/v1/demo/diagnostics/error", false }, // anonymous group and endpoint
    };

    [Theory(DisplayName = "An operation lists security schemes only when it requires authorization")]
    [MemberData(nameof(OperationSecurity))]
    public async Task OperationSecurityFollowsTheAuthorizationMetadata(string path, bool expectedSecured)
    {
        // Arrange
        using var document = await GetDocumentAsync("v1");

        // Act
        var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("get");
        var secured = operation.TryGetProperty("security", out var security) && security.GetArrayLength() > 0;

        // Assert
        Assert.Equal(expectedSecured, secured);
    }

    public static TheoryData<string, string[]> SchemesByEndpoint => new()
    {
        { "/v1/demo/auth/either", ["Bearer", "OAuth2", "OIDC", "X-Api-Key"] }, // nothing pinned: every scheme
        { "/v1/demo/auth/api-key-only", ["X-Api-Key"] }, // pinned to the API key
        { "/v1/demo/auth/bearer-only", ["Bearer", "OAuth2", "OIDC"] }, // pinned to bearer tokens: the token schemes
    };

    [Theory(DisplayName = "An operation lists only the security schemes its endpoint accepts")]
    [MemberData(nameof(SchemesByEndpoint))]
    public async Task OperationListsOnlyTheSchemesItAccepts(string path, string[] expected)
    {
        // Arrange
        using var document = await GetDocumentAsync("v1");

        // Act
        var schemes = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty("get").GetProperty("security")
            .EnumerateArray()
            .SelectMany(requirement => requirement.EnumerateObject().Select(x => x.Name))
            .Order()
            .ToArray();

        // Assert
        Assert.Equal(expected, schemes);
    }

    [Fact(DisplayName = "The document does not apply security to every operation")]
    public async Task DocumentHasNoGlobalSecurity()
    {
        // Arrange
        using var document = await GetDocumentAsync("v1");

        // Act
        var hasGlobalSecurity = document.RootElement.TryGetProperty("security", out _);

        // Assert
        Assert.False(hasGlobalSecurity);
    }

    [Fact(DisplayName = "Each API version has its own document, and unknown versions have none")]
    public async Task EachVersionHasItsOwnDocument()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var v1 = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        var v2 = await client.GetAsync("/openapi/v2.json", TestContext.Current.CancellationToken);
        var v3 = await client.GetAsync("/openapi/v3.json", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, v1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, v2.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, v3.StatusCode);
    }

    [Fact(DisplayName = "Query parameters are documented in camel case like the JSON, and headers keep their names")]
    public async Task QueryParametersAreDocumentedInCamelCase()
    {
        // Arrange
        using var document = await GetDocumentAsync("v1");

        // Act
        var parameters = document.RootElement
            .GetProperty("paths").GetProperty("/v1/demo/pagination/people").GetProperty("get").GetProperty("parameters")
            .EnumerateArray()
            .Select(x => (Name: x.GetProperty("name").GetString(), In: x.GetProperty("in").GetString()))
            .ToList();

        // Assert
        Assert.Contains(("pageSize", "query"), parameters);
        Assert.Contains(("orderBy", "query"), parameters);
        Assert.Contains(("createdFrom", "query"), parameters);
        Assert.DoesNotContain(parameters, x => x.In == "query" && char.IsUpper(x.Name![0]));
        Assert.Contains(("Correlation-Id", "header"), parameters);
    }

    public static TheoryData<string, string, bool?> IdempotencyKeyHeaders => new()
    {
        { "/v1/demo/idempotency/orders", "post", false }, // marked WithIdempotency(): optional
        { "/v1/demo/idempotency/payments", "post", true }, // marked WithIdempotency(required: true)
        { "/v1/demo/pagination/people", "get", null }, // not idempotent: no header
        { "/v1/demo/results/orders", "post", null }, // a POST without the mark: no header
    };

    [Theory(DisplayName = "The Idempotency-Key header is documented only where the endpoint uses it, as required only when it must be sent")]
    [MemberData(nameof(IdempotencyKeyHeaders))]
    public async Task IdempotencyKeyHeaderFollowsTheEndpointMetadata(string path, string method, bool? expectedRequired)
    {
        // Arrange
        using var document = await GetDocumentAsync("v1");

        // Act
        var header = document.RootElement
            .GetProperty("paths").GetProperty(path).GetProperty(method).GetProperty("parameters")
            .EnumerateArray()
            .Where(x => x.GetProperty("name").GetString() == "Idempotency-Key")
            .Select(x => (bool?)(x.TryGetProperty("required", out var required) && required.GetBoolean()))
            .SingleOrDefault();

        // Assert
        Assert.Equal(expectedRequired, header);
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
