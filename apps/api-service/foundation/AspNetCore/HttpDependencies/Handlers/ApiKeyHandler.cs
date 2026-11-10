using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.HttpDependencies.Models;

namespace Aurore.Foundation.AspNetCore.HttpDependencies.Handlers;

/// <summary>Adds a static authentication header to every request.</summary>
public sealed class ApiKeyHandler(UpstreamApiKeyOptions options) : DelegatingHandler
{
    /// <summary>Adds the configured header before sending the request.</summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(options.HeaderName);
        request.Headers.TryAddWithoutValidation(options.HeaderName, options.Value);

        return base.SendAsync(request, cancellationToken);
    }
}
