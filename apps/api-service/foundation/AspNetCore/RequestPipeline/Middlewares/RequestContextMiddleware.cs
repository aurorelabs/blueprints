using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Constants;
using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.Core.Constants;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.Models;
using Aurore.Foundation.Core.Privacy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;

/// <summary>
/// Fills the RequestContext once per request. The correlation id is the incoming Correlation-Id header, else X-Request-ID,
/// else the trace id; invalid values are ignored. Personal data (client address, user agent) is encrypted in the log
/// scope when privacy is on; the context keeps the real values.
/// </summary>
public sealed class RequestContextMiddleware(RequestDelegate next, IPrivacyProtector privacy, ILogger<RequestContextMiddleware> logger)
{
    internal const int MinCorrelationIdLength = 12;
    internal const int MaxCorrelationIdLength = 64;
    internal const string CorrelationIdPattern = "^[A-Za-z0-9._-]{12,64}$";
    internal const int MinIdempotencyKeyLength = 12;
    internal const int MaxIdempotencyKeyLength = 128;
    internal const string IdempotencyKeyPattern = "^[A-Za-z0-9._-]{12,128}$";
    internal const int MaxHeaderValueLength = 256;

    /// <summary>Fills the request context, tags the span, echoes the correlation id and opens the log scope.</summary>
    public async Task InvokeAsync(HttpContext context, RequestContext requestContext)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var correlationId = ValidToken(context.GetRequestHeader(CustomHeaderNames.CorrelationId), MinCorrelationIdLength, MaxCorrelationIdLength)
            ?? ValidToken(context.GetRequestHeader(CustomHeaderNames.RequestId), MinCorrelationIdLength, MaxCorrelationIdLength)
            ?? traceId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CustomHeaderNames.CorrelationId] = correlationId;

            return Task.CompletedTask;
        });

        var rawIdempotencyKey = context.GetRequestHeader(CustomHeaderNames.IdempotencyKey);
        var idempotencyKey = ValidToken(rawIdempotencyKey, MinIdempotencyKeyLength, MaxIdempotencyKeyLength);

        if (rawIdempotencyKey.HasValue() && idempotencyKey is null)
        {
            await Results
                .Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Idempotency-Key",
                    detail: $"Use {MinIdempotencyKeyLength} to {MaxIdempotencyKeyLength} letters, digits, '-', '_' or '.'.")
                .ExecuteAsync(context);

            return;
        }

        var clientAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = Truncate(context.Request.Headers.UserAgent.ToString());
        var host = Truncate(context.Request.Host.Value);

        requestContext.Initialize(correlationId, traceId, Activity.Current?.SpanId.ToString(), idempotencyKey, clientAddress, userAgent, host);

        Activity.Current?.SetTag(OpenTelemetryProperties.Attributes.Custom.CorrelationId, correlationId);

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            [OpenTelemetryProperties.Attributes.Custom.CorrelationId] = correlationId,
            [OpenTelemetryProperties.Attributes.Standard.ServerAddress] = host,
            [OpenTelemetryProperties.Attributes.Standard.UserAgentOriginal] = privacy.Protect(OpenTelemetryProperties.Attributes.Standard.UserAgentOriginal, userAgent),
            [OpenTelemetryProperties.Attributes.Standard.ClientAddress] = privacy.Protect(OpenTelemetryProperties.Attributes.Standard.ClientAddress, clientAddress),
        });

        await next(context);
    }

    /// <summary>
    /// Returns the value when it is minLength..maxLength characters of letters, digits, '-', '_' or '.'; otherwise null.
    /// </summary>
    private static string? ValidToken(string value, int minLength, int maxLength)
    {
        if (value.Length < minLength || value.Length > maxLength)
            return null;

        foreach (var c in value)
        {
            if (char.IsAsciiLetterOrDigit(c) is false && c is not ('-' or '_' or '.'))
                return null;
        }

        return value;
    }

    private static string? Truncate(string? value)
    {
        return value is { Length: > MaxHeaderValueLength } ? value[..MaxHeaderValueLength] : value;
    }
}
