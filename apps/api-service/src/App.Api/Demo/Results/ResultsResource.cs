using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Results;

/// <summary>
/// Results and errors: a feature returns Result&lt;T&gt; (or an error), and ToHttpResult() turns it into the response.
/// Try: order 1 (pending), order 2 (shipped), order 99 (missing), a bad body, or a repeated email.
/// </summary>
internal sealed class ResultsResource() : DemoResource("results", "Results and errors")
{
    public override bool RequireAuthorization => false;
}

internal sealed class GetDemoOrderEndpoint : MinimalEndpoint<ResultsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("orders/{id:int}", Handle);

        static Results<Ok<DemoOrder>, ProblemHttpResult> Handle(int id)
        {
            return DemoOrders.Get(id).ToHttpResult();
        }
    }
}

/// <summary>
/// The request is validated before the handler runs, by the global validation filter (400 with field errors), so the handler
/// never validates by hand. The feature can still answer a conflict (409), and success is a 201 Created.
/// </summary>
internal sealed class CreateDemoOrderEndpoint : MinimalEndpoint<ResultsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapPost("orders", Handle);

        static Results<Created<DemoOrder>, ProblemHttpResult> Handle(CreateDemoOrderRequest request)
        {
            return DemoOrders
                .Create(request)
                .ToHttpResult(order => TypedResults.Created($"/v1/demo/results/orders/{order.Id}", order));
        }
    }
}

/// <summary>
/// A business rule checked with FluentValidation that fails as a 409 Conflict, with the field details attached.
/// </summary>
internal sealed class CancelDemoOrderEndpoint : MinimalEndpoint<ResultsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapPost("orders/{id:int}/cancel", Handle);

        static Results<Ok<DemoOrder>, ProblemHttpResult> Handle(int id)
        {
            return DemoOrders.Cancel(id).ToHttpResult();
        }
    }
}

/// <summary>
/// A result without a value: success is 204 No Content.
/// </summary>
internal sealed class DeleteDemoOrderEndpoint : MinimalEndpoint<ResultsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapDelete("orders/{id:int}", Handle);

        static Results<NoContent, ProblemHttpResult> Handle(int id)
        {
            return DemoOrders.Delete(id).ToHttpResult();
        }
    }
}
