using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Integration.Support;

/// <summary>
/// Access tokens of the users of the realm "aurorelabs" of the sandbox Keycloak (alice, bob, carol, dave; the password is
/// the name). They are asked from the address the application has as its authority (auth.localhost:9999): the issuer in the
/// token has to match it.
/// </summary>
public static class KeycloakTokens
{
    private const string Authority = "http://auth.localhost:9999/realms/aurorelabs";
    private const string ComposeHint = "Start the sandbox infrastructure first (.sandbox/docker-compose.yaml: PostgreSQL, Keycloak, nginx), see docs/instructions/testing.md.";

    /// <summary>Asks Keycloak for the access token of a user. Fails with a hint to start the sandbox when Keycloak is not there.</summary>
    public static async Task<string> RequestAsync(string user, CancellationToken cancellationToken)
    {
        using var http = new HttpClient(Ipv4Handler.Create()) { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            using var response = await http.PostAsync(
                $"{Authority}/protocol/openid-connect/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = "api-tests",
                    ["grant_type"] = "password",
                    ["username"] = user,
                    ["password"] = user,
                }),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

            return body.RootElement.GetProperty("access_token").GetString()!;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new InvalidOperationException($"Could not get a token for '{user}' from Keycloak. {ComposeHint}", ex);
        }
    }
}
