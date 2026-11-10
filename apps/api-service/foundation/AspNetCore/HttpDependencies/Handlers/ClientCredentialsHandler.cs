using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.HttpDependencies.Services;
using Aurore.Foundation.Core.HttpDependencies.Models;

namespace Aurore.Foundation.AspNetCore.HttpDependencies.Handlers;

/// <summary>Adds a cached OAuth2 client-credentials access token as <c>Authorization: Bearer</c>.</summary>
public sealed class ClientCredentialsHandler(ClientCredentialsTokenCache tokens, string dependency, ClientCredentialsOptions options) : DelegatingHandler
{
    /// <summary>Adds a valid access token before sending the request.</summary>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.GetTokenAsync(dependency, options, cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
