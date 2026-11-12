using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Integration.Authentication;

/// <summary>The API key scheme end to end, with the local-tester client of appsettings.json (role reader, scope api.read).</summary>
[Trait("Feature", "API key authentication")]
public sealed class ApiKeyAuthenticationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string HeaderName = "X-Api-Key";
    private const string LocalKey = "local-dev-api-key";

    public static TheoryData<string, string?, HttpStatusCode> Access => new()
    {
        { "/v1/demo/auth/api-key-only", LocalKey, HttpStatusCode.OK }, // the client has the reader role
        { "/v1/demo/auth/either", LocalKey, HttpStatusCode.OK }, // the client has the api.read scope
        { "/v1/demo/auth/scope", LocalKey, HttpStatusCode.OK },
        { "/v1/demo/auth/role", LocalKey, HttpStatusCode.Forbidden }, // needs admin, which the client lacks
        { "/v1/demo/auth/combined", LocalKey, HttpStatusCode.Forbidden }, // needs api.write and admin
        { "/v1/demo/auth/bearer-only", LocalKey, HttpStatusCode.Unauthorized }, // pinned to tokens: a key is not one
        { "/v1/demo/auth/api-key-only", "not-the-key", HttpStatusCode.Unauthorized },
        { "/v1/demo/auth/api-key-only", null, HttpStatusCode.Unauthorized },
    };

    [Theory(DisplayName = "A key opens what its client's roles and scopes allow, and a wrong or missing key opens nothing")]
    [MemberData(nameof(Access))]
    public async Task KeyOpensWhatItsClientAllows(string path, string? key, HttpStatusCode expected)
    {
        // Arrange
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (key is not null)
            request.Headers.Add(HeaderName, key);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact(DisplayName = "A refused key is challenged with the scheme and the header to send it in")]
    public async Task RefusedKeyIsChallengedWithTheHeader()
    {
        // Arrange
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/demo/auth/api-key-only");
        request.Headers.Add(HeaderName, "not-the-key");

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("ApiKey header=\"X-Api-Key\"", response.Headers.WwwAuthenticate.ToString());
    }
}
