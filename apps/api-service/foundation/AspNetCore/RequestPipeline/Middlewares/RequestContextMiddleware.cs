using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Constants;
using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.Models;
using Aurore.Foundation.Telemetry.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;

public sealed class RequestContextMiddleware(RequestDelegate next, ILogger<RequestContextMiddleware> logger)
{
    internal const int MaxCorrelationIdLength = 64;
    internal const string CorrelationIdPattern = "^[A-Za-z0-9._-]{1,64}$";

    public async Task InvokeAsync(HttpContext context, RequestContext requestContext)
    {
        var correlationId = SanitizeCorrelationId(context.GetRequestHeader(CustomHeaderNames.CorrelationId));
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var spanId = Activity.Current?.SpanId.ToString();
        var traceParent = Activity.Current?.Id;
        var idempotencyKey = context.GetRequestHeader(CustomHeaderNames.IdempotencyKey);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CustomHeaderNames.CorrelationId] = correlationId;

            if (traceParent.HasValue())
                context.Response.Headers[HeaderNames.TraceParent] = traceParent;

            return Task.CompletedTask;
        });

        requestContext.Update(correlationId, traceId, spanId);
        requestContext.UpdateIdempotencyKey(idempotencyKey.HasValue() ? idempotencyKey : null);

        // GDPR: client.address and user_agent.original are personal data; revisit with the logging/encryption discussion.
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            [OpenTelemetryProperties.Attributes.Custom.CorrelationId] = correlationId,
            [OpenTelemetryProperties.Attributes.Standard.ServerAddress] = context.Request.Host.Value,
            [OpenTelemetryProperties.Attributes.Standard.UserAgentOriginal] = context.Request.Headers.UserAgent.ToString(),
            [OpenTelemetryProperties.Attributes.Standard.ClientAddress] = context.Connection.RemoteIpAddress?.ToString(),
        });

        await next(context);
    }

    private static string SanitizeCorrelationId(string value)
    {
        if (value.Length is 0 or > MaxCorrelationIdLength)
            return Guid.NewGuid().ToString("D");

        foreach (var c in value)
        {
            if (char.IsAsciiLetterOrDigit(c) is false && c is not ('-' or '_' or '.'))
                return Guid.NewGuid().ToString("D");
        }

        return value;
    }
}
