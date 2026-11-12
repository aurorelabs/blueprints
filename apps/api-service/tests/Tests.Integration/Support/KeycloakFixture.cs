using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Integration.Support;

/// <summary>
/// Real tokens from the Keycloak of the sandbox (realm "aurorelabs"). The application runs with its own
/// appsettings.json, which already points at that realm, so what it validates is what Keycloak issues.
/// </summary>
public sealed class KeycloakFixture : IAsyncLifetime
{
    private readonly Dictionary<string, string> _tokens = [];

    /// <summary>The application, as configured by its appsettings.json.</summary>
    public WebApplicationFactory<Program> Factory { get; } = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
        .ConfigureTestServices(services => services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            options.BackchannelHttpHandler = Ipv4Handler.Create())));

    /// <summary>The access token of the user (alice or bob).</summary>
    public string TokenOf(string user)
    {
        return _tokens[user];
    }

    public async ValueTask InitializeAsync()
    {
        foreach (var user in new[] { "alice", "bob" })
            _tokens[user] = await KeycloakTokens.RequestAsync(user, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}
