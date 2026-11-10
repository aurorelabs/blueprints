using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Timeouts;

/// <summary>
/// A request timeout. The handler passes the CancellationToken on, which is what makes the timeout effective.
/// </summary>
internal sealed class TimeoutsResource() : DemoResource("timeouts", "Request timeouts")
{
    public override bool RequireAuthorization => false;
}

internal sealed class GetTimeoutEndpoint : MinimalEndpoint<TimeoutsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("{seconds:int}", HandleAsync)
            .WithMetadata(new RequestTimeoutAttribute(milliseconds: 2000));

        static async Task<IResult> HandleAsync(int seconds, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);

            return TypedResults.Ok(new { WaitedSeconds = seconds });
        }
    }
}
