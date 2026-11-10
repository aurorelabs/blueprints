using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.HttpDependencies.Models;
using Microsoft.AspNetCore.Http;

namespace Aurore.Foundation.AspNetCore.HttpDependencies.Services;

/// <summary>
/// Requests OAuth2 client-credentials tokens (RFC 6749 §4.4) and caches them per key until shortly before they expire.
/// Singleton because HttpClient handlers are recreated every few minutes and would otherwise lose the cache.
/// </summary>
public sealed class ClientCredentialsTokenCache(IHttpClientFactory clientFactory, TimeProvider timeProvider)
{
    /// <summary>Internal IHttpClientFactory name of the client that calls token endpoints; never sent anywhere.</summary>
    public const string TokenEndpointClientName = "oauth2-token-endpoint";

    private static readonly TimeSpan _refreshMargin = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<string, CachedToken> _tokens = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>
    /// Returns the cached access token for the key, requesting a new one when there is none or it is about to expire.
    /// </summary>
    public async ValueTask<string> GetTokenAsync(string key, ClientCredentialsOptions options, CancellationToken cancellationToken)
    {
        if (TryGetValid(key, out var token))
            return token;

        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken);

        try
        {
            if (TryGetValid(key, out token))
                return token;

            var response = await RequestTokenAsync(key, options, cancellationToken);
            var lifetime = TimeSpan.FromSeconds(response.ExpiresIn);
            var refreshIn = lifetime > _refreshMargin * 2 ? lifetime - _refreshMargin : lifetime / 2;

            _tokens[key] = new CachedToken(response.AccessToken, timeProvider.GetUtcNow() + refreshIn);

            return response.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryGetValid(string key, out string token)
    {
        if (_tokens.TryGetValue(key, out var cached) && cached.RefreshAt > timeProvider.GetUtcNow())
        {
            token = cached.AccessToken;

            return true;
        }

        token = string.Empty;

        return false;
    }

    private async Task<TokenResponse> RequestTokenAsync(string key, ClientCredentialsOptions options, CancellationToken cancellationToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
        };

        if (options.Scopes.Length > 0)
            form["scope"] = string.Join(' ', options.Scopes);

        var client = clientFactory.CreateClient(TokenEndpointClientName);

        using var response = await client.PostAsync(options.TokenUrl, new FormUrlEncodedContent(form), cancellationToken);

        if (response.IsSuccessStatusCode is false)
            throw new HttpRequestException($"Token request for '{key}' failed with status code {(int)response.StatusCode}.", null, response.StatusCode);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        return token is { AccessToken.Length: > 0 }
            ? token
            : throw new HttpRequestException($"Token response for '{key}' has no access_token.");
    }

    private sealed record CachedToken(string AccessToken, DateTimeOffset RefreshAt);

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
