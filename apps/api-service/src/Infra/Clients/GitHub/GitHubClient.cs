using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Clients.GitHub;

namespace Aurore.Blueprints.ApiService.Infra.Clients.GitHub;

internal sealed class GitHubClient(HttpClient http) : IGitHubClient
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<GitHubUser?> GetUserAsync(string login, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync($"users/{Uri.EscapeDataString(login)}", cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GitHubUser>(_json, cancellationToken);
    }
}
