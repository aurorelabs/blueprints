using System;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Idempotency;

/// <summary>
/// Repeating a POST with the same Idempotency-Key replays the first result.
/// </summary>
internal sealed class IdempotencyResource() : DemoResource("idempotency", "Idempotency")
{
    public override bool RequireAuthorization => false;
}

internal sealed class PostOrderEndpoint : MinimalEndpoint<IdempotencyResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapPost("orders", () => TypedResults.Ok(new { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow }))
            .WithIdempotency();
    }
}

/// <summary>
/// The Idempotency-Key header is mandatory here (400 without it). The replay keeps Location and the custom header.
/// </summary>
internal sealed class PostPaymentEndpoint : MinimalEndpoint<IdempotencyResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapPost("payments", Handle)
            .WithIdempotency(required: true);

        static IResult Handle(HttpContext context)
        {
            var id = Guid.NewGuid();
            context.Response.Headers["X-Payment-Provider"] = "demo";

            return TypedResults.Created($"/v1/demo/idempotency/payments/{id}", new { Id = id });
        }
    }
}
