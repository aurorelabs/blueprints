using System;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Caching;

/// <summary>
/// A named output cache policy from RequestPipeline:OutputCache:Policies ("short": 10 seconds).
/// </summary>
internal sealed class CachingResource() : DemoResource("caching", "Output caching")
{
    public override bool RequireAuthorization => false;
}

internal sealed class GetTimestampEndpoint : MinimalEndpoint<CachingResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("timestamp", () => TypedResults.Ok(new { Timestamp = DateTime.UtcNow }))
            .CacheOutput("short");
    }
}
