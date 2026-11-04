using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Clients.GitHub;
using Aurore.Blueprints.ApiService.Core.Constants;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;
using Aurore.Foundation.Core.Application;
using Aurore.Foundation.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Resources.Tests.Endpoints;

internal sealed class GetCorrelationEndpoint : MinimalEndpoint<TestsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("correlation", Handle);

        static IResult Handle(RequestContext context)
        {
            return TypedResults.Ok(new
            {
                context.CorrelationId,
                context.TraceId,
                context.OriginalSpanId,
                CurrentSpanId = Activity.Current?.SpanId.ToString(),
                Version = AppVersion.Current,
            });
        }
    }
}

internal sealed class GetGitHubUserEndpoint : MinimalEndpoint<TestsResource>
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

internal sealed class GetRandomUserEndpoint : MinimalEndpoint<TestsResource>
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

internal sealed class GetRateLimitedEndpoint : MinimalEndpoint<TestsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("rate-limited", () => TypedResults.Ok(new { Timestamp = DateTime.UtcNow }))
            .RequireRateLimiting("tests");
    }
}

internal sealed class GetTimeoutEndpoint : MinimalEndpoint<TestsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("timeout/{seconds:int}", HandleAsync)
            .WithMetadata(new RequestTimeoutAttribute(milliseconds: 2000));

        static async Task<IResult> HandleAsync(int seconds, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);

            return TypedResults.Ok(new { WaitedSeconds = seconds });
        }
    }
}

internal sealed class PostIdempotentEndpoint : MinimalEndpoint<TestsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapPost("idempotent", () => TypedResults.Ok(new { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }))
            .WithAuroreIdempotency();
    }
}

internal sealed class GetErrorEndpoint : MinimalEndpoint<TestsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("error", Handle);

        static IResult Handle()
        {
            throw new InvalidOperationException("Test exception from the tests resource.");
        }
    }
}

internal sealed class GetAuthorizedEndpoint : MinimalEndpoint<TestsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("authorized", Handle)
            .RequireAuthorization();

        static IResult Handle(ClaimsPrincipal user)
        {
            return TypedResults.Ok(new { Subject = user.FindFirstValue(ClaimTypes.NameIdentifier), user.Identity?.Name });
        }
    }
}
