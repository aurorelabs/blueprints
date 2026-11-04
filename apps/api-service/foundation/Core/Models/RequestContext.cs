using System.Diagnostics;

namespace Aurore.Foundation.Core.Models;

public sealed class RequestContext
{
    public string CorrelationId { get; private set; } = string.Empty;

    public string TraceId { get; private set; } = string.Empty;

    /// <summary>
    /// The span of the incoming request, captured once when the request started.
    /// </summary>
    public string? OriginalSpanId { get; private set; }

    /// <summary>
    /// The span active right now; it changes as child spans start and stop. Falls back to the request's span.
    /// </summary>
    public string? SpanId => Activity.Current?.SpanId.ToString() ?? OriginalSpanId;

    public string? IdempotencyKey { get; private set; }

    public void Update(string correlationId, string traceId, string? spanId)
    {
        CorrelationId = correlationId;
        TraceId = traceId;
        OriginalSpanId ??= spanId;
    }

    public void UpdateIdempotencyKey(string? idempotencyKey)
    {
        IdempotencyKey = idempotencyKey;
    }
}
