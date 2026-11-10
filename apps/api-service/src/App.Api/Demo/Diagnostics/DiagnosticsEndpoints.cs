using System;
using System.Diagnostics;
using System.Security.Claims;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.Core.Application;
using Aurore.Foundation.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Diagnostics;

internal sealed class GetCorrelationEndpoint : MinimalEndpoint<DiagnosticsResource>
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
                context.ClientAddress,
                context.UserAgent,
                context.Host,
                context.UserId,
                CurrentSpanId = Activity.Current?.SpanId.ToString(),
                Version = AppVersion.Current,
            });
        }
    }
}

internal sealed class GetErrorEndpoint : MinimalEndpoint<DiagnosticsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("error", Handle);

        static IResult Handle()
        {
            throw new InvalidOperationException("Test exception from the diagnostics resource.");
        }
    }
}

internal sealed class GetAuthorizedEndpoint : MinimalEndpoint<DiagnosticsResource>
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

/// <summary>
/// Writes one log line from inside the request, so the log scope (correlation id, client address, user agent, user id)
/// can be seen. With Privacy:Enabled the personal fields appear encrypted.
/// </summary>
internal sealed class GetLogEndpoint : MinimalEndpoint<DiagnosticsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder.MapGet("log", Handle);

        static IResult Handle(ILogger<GetLogEndpoint> logger)
        {
            logger.LogInformation("Demo log line written inside the request.");

            return TypedResults.NoContent();
        }
    }
}
