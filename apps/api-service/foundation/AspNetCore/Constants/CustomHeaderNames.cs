namespace Aurore.Foundation.AspNetCore.Constants;

/// <summary>Names of the request and response headers the foundation defines.</summary>
public static class CustomHeaderNames
{
    /// <summary>Carries the correlation id in requests and responses.</summary>
    public const string CorrelationId = "Correlation-Id";
    /// <summary>Accepted as a correlation id when Correlation-Id is not sent.</summary>
    public const string RequestId = "X-Request-ID";
    /// <summary>The key a client sends to make a request safe to retry.</summary>
    public const string IdempotencyKey = "Idempotency-Key";
    /// <summary>Set to true on a response that was replayed from an earlier request with the same key.</summary>
    public const string IdempotencyReplayed = "Idempotency-Replayed";
}
