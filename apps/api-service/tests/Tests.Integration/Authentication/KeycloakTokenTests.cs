using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Tests.Integration.Support;

namespace Tests.Integration.Authentication;

/// <summary>What the application does with real Keycloak tokens: validation, roles from the realm and from the client, scopes.</summary>
[Trait("Feature", "Keycloak tokens")]
public sealed class KeycloakTokenTests(KeycloakFixture keycloak) : IClassFixture<KeycloakFixture>
{
    private const string RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    public static TheoryData<string, string, HttpStatusCode> Access => new()
    {
        { "alice", "/v1/demo/auth/authenticated", HttpStatusCode.OK }, // any valid token
        { "alice", "/v1/demo/auth/scope", HttpStatusCode.OK }, // scope api.read
        { "alice", "/v1/demo/auth/role", HttpStatusCode.Forbidden }, // needs the admin role
        { "alice", "/v1/demo/auth/combined", HttpStatusCode.Forbidden }, // needs api.write and admin
        { "alice", "/v1/demo/auth/bearer-only", HttpStatusCode.OK },
        { "alice", "/v1/demo/auth/api-key-only", HttpStatusCode.Unauthorized }, // a token is not an API key
        { "bob", "/v1/demo/auth/role", HttpStatusCode.OK },
        { "bob", "/v1/demo/auth/combined", HttpStatusCode.OK },
    };

    [Theory(DisplayName = "A real token opens the endpoints its roles and scopes allow, and no others")]
    [MemberData(nameof(Access))]
    public async Task TokenOpensWhatItsRolesAndScopesAllow(string user, string path, HttpStatusCode expected)
    {
        // Arrange
        using var client = CreateClient(user);

        // Act
        using var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact(DisplayName = "Without a token the endpoint answers 401")]
    public async Task WithoutATokenTheAnswerIs401()
    {
        // Arrange
        using var client = CreateClient(user: null);

        // Act
        using var response = await client.GetAsync("/v1/demo/auth/authenticated", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "A tampered token is refused")]
    public async Task TamperedTokenIsRefused()
    {
        // Arrange
        using var client = keycloak.Factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", keycloak.TokenOf("alice")[..^4] + "AAAA");

        // Act
        using var response = await client.GetAsync("/v1/demo/auth/authenticated", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "Realm roles and the roles of the API client both become role claims")]
    public async Task RolesFromTheRealmAndTheClientBecomeRoleClaims()
    {
        // Arrange
        var alice = await RolesOfAsync("alice");
        var bob = await RolesOfAsync("bob");

        // Act
        // Assert
        Assert.Equal(["reader"], alice);
        Assert.Equal(["admin", "editor"], bob);
    }

    [Fact(DisplayName = "The token's scope claim becomes the scopes of the caller")]
    public async Task ScopeClaimBecomesScopes()
    {
        // Arrange
        using var client = CreateClient("alice");

        // Act
        using var response = await client.GetAsync("/v1/demo/auth/authenticated", TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        var scope = body.RootElement.GetProperty("claims").EnumerateArray()
            .Where(x => x.GetProperty("type").GetString() == "scope")
            .Select(x => x.GetProperty("value").GetString())
            .Single();
        Assert.Contains("api.read", scope!.Split(' '));
    }

    [Fact(DisplayName = "The subject of the token is the caller's name identifier")]
    public async Task SubjectIsTheNameIdentifier()
    {
        // Arrange
        using var client = CreateClient("alice");

        // Act
        using var response = await client.GetAsync("/v1/demo/auth/authenticated", TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.True(Guid.TryParse(body.RootElement.GetProperty("subject").GetString(), out _));
    }

    private async Task<string[]> RolesOfAsync(string user)
    {
        using var client = CreateClient(user);
        using var response = await client.GetAsync("/v1/demo/auth/authenticated", TestContext.Current.CancellationToken);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return [.. body.RootElement.GetProperty("claims").EnumerateArray()
            .Where(x => x.GetProperty("type").GetString() == RoleClaimType)
            .Select(x => x.GetProperty("value").GetString()!)
            .Order()];
    }

    private HttpClient CreateClient(string? user)
    {
        var client = keycloak.Factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        if (user is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", keycloak.TokenOf(user));

        return client;
    }
}
