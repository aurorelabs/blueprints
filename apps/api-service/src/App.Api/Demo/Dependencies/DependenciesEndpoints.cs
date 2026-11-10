using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Clients.GitHub;
using Aurore.Blueprints.ApiService.Core.Constants;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Dependencies;

internal sealed class GetGitHubUserEndpoint : MinimalEndpoint<DependenciesResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("github/{user}", HandleAsync);

        static async Task<IResult> HandleAsync(string user, IGitHubClient gitHub, CancellationToken cancellationToken)
        {
            var result = await gitHub.GetUserAsync(user, cancellationToken);

            return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
        }
    }
}

internal sealed class GetRandomUserEndpoint : MinimalEndpoint<DependenciesResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("random-user", HandleAsync);

        static async Task<IResult> HandleAsync(IHttpClientFactory clientFactory, CancellationToken cancellationToken)
        {
            var http = clientFactory.CreateClient(HttpDependencyNames.RandomUser);

            using var response = await http.GetAsync("api/?results=1", cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            return TypedResults.Content(body, "application/json", statusCode: (int)response.StatusCode);
        }
    }
}
