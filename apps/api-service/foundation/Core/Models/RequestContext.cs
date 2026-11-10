using System;
using System.Diagnostics;

namespace Aurore.Foundation.Core.Models;

/// <summary>
/// Facts about the current request, readable from any service. Filled once by the request pipeline and then read-only,
/// except the user id, which becomes known after authentication and can be set once. These are the real values:
/// encryption of personal data happens only when they are written to logs and traces.
/// To carry something more (for example a tenant id), add a property here and fill it in the pipeline.
/// </summary>
public sealed class RequestContext
{
    private bool _initialized;

    /// <summary>
    /// The id that ties together everything done for this request: the caller's Correlation-Id or X-Request-ID, else the
    /// trace id.
    /// </summary>
    public string CorrelationId { get; private set; } = string.Empty;

    /// <summary>The id of the distributed trace this request belongs to.</summary>
    public string TraceId { get; private set; } = string.Empty;

    /// <summary>
    /// The span of the incoming request, captured once when the request started.
    /// </summary>
    public string? OriginalSpanId { get; private set; }

    /// <summary>
    /// The span active right now; it changes as child spans start and stop. Falls back to the request's span.
    /// </summary>
    public string? SpanId => Activity.Current?.SpanId.ToString() ?? OriginalSpanId;

    /// <summary>
    /// The validated Idempotency-Key header, or null when the request did not send one.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>The caller's IP address, after forwarded headers were applied. Personal data.</summary>
    public string? ClientAddress { get; private set; }

    /// <summary>The caller's User-Agent header, cut to 256 characters. Personal data.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>The host name the request was sent to, cut to 256 characters.</summary>
    public string? Host { get; private set; }

    /// <summary>
    /// Null for anonymous requests and until authentication has run.
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>Fills the context. It can only be called once per request; a second call throws.</summary>
    public void Initialize(
        string correlationId,
        string traceId,
        string? spanId,
        string? idempotencyKey,
        string? clientAddress,
        string? userAgent,
        string? host)
    {
        if (_initialized)
            throw new InvalidOperationException("The request context is already initialized.");

        _initialized = true;
        CorrelationId = correlationId;
        TraceId = traceId;
        OriginalSpanId = spanId;
        IdempotencyKey = idempotencyKey;
        ClientAddress = clientAddress;
        UserAgent = userAgent;
        Host = host;
    }

    /// <summary>Sets the authenticated user's id. It can only be set once.</summary>
    public void SetUserId(string userId)
    {
        if (UserId is not null)
            throw new InvalidOperationException("The user id is already set.");

        UserId = userId;
    }
}
