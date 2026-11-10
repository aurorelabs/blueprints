using System;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.RateLimiting;

/// <summary>
/// A named rate limit policy from RequestPipeline:RateLimiting:Policies ("tests": 3 requests per 10 seconds).
/// </summary>
internal sealed class RateLimitingResource() : DemoResource("rate-limiting", "Rate limiting")
{
    public override bool RequireAuthorization => false;
}

internal sealed class GetLimitedEndpoint : MinimalEndpoint<RateLimitingResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("limited", () => TypedResults.Ok(new { Timestamp = DateTime.UtcNow }))
            .RequireRateLimiting("tests");
    }
}
