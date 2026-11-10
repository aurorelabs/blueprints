using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Constants;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.AspNetCore.HttpDependencies.Handlers;

/// <summary>Forwards the current request's correlation id to the upstream service.</summary>
public sealed class CorrelationIdPropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    /// <summary>
    /// IHttpClientFactory creates handlers in their own DI scope, so the request's RequestContext is read through the
    /// current HttpContext instead of being injected. Outside a request (background work) nothing is added.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = httpContextAccessor.HttpContext?.RequestServices.GetService<RequestContext>()?.CorrelationId;

        if (correlationId.HasValue() && request.Headers.Contains(CustomHeaderNames.CorrelationId) is false)
            request.Headers.TryAddWithoutValidation(CustomHeaderNames.CorrelationId, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
