using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Constants;
using Aurore.Foundation.Core.Models;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Filters;

/// <summary>
/// Replays the first successful (2xx) result when a caller repeats a request with the same Idempotency-Key.
/// The key is scoped to the caller, method and path; the same key always means the same request, whatever the body says. Replays
/// carry the stored status, JSON body and the headers the endpoint added (plus Location of Created/Accepted results),
/// and an Idempotency-Replayed header. Per-request headers (Correlation-Id, Date, ...) are those of the new request.
/// This is a convenience for clients that retry; guarantee uniqueness in the database too.
/// </summary>
public sealed class IdempotencyFilter(HybridCache cache, IdempotencyOptions options, RequestContext requestContext) : IEndpointFilter
{
    /// <summary>
    /// Runs the endpoint the first time a key is seen and replays the stored result when the same caller repeats it.
    /// </summary>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var key = requestContext.IdempotencyKey;

        var http = context.HttpContext;

        if (key is null)
        {
            if (http.GetEndpoint()?.Metadata.GetMetadata<IdempotencyMetadata>() is { Required: true })
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Idempotency-Key required",
                    detail: $"Send the {CustomHeaderNames.IdempotencyKey} header with a unique value for this request.");
            }

            return await next(context);
        }

        var cacheKey = $"{CustomHeaderNames.IdempotencyKey}:{GetCaller(http)}:{http.Request.Method}:{http.Request.Path}:{key}";

        object? original = null;
        var executed = false;

        try
        {
            var entry = await cache.GetOrCreateAsync(
                cacheKey,
                async _ =>
                {
                    var headersBefore = SnapshotHeaders(http);

                    original = await next(context);
                    executed = true;

                    // Only 2xx results are stored; throwing keeps GetOrCreateAsync from caching anything, so a failed request can be retried.
                    return Capture(http, original, headersBefore) ?? throw new NotCacheableResultException();
                },
                new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(options.CacheDurationSeconds) },
                cancellationToken: http.RequestAborted);

            if (executed)
                return original;


            return new ReplayedResult(entry);
        }
        catch (NotCacheableResultException)
        {
            return original;
        }
    }

    /// <summary>
    /// The key is scoped to the caller: two callers sending the same Idempotency-Key never see each other's result.
    /// </summary>
    private static string GetCaller(HttpContext context)
    {
        return context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";
    }

    private static Dictionary<string, string> SnapshotHeaders(HttpContext http)
    {
        return http.Response.Headers.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    private static IdempotencyCacheEntry? Capture(HttpContext http, object? result, Dictionary<string, string> headersBefore)
    {
        while (result is INestedHttpResult nested)
        {
            result = nested.Result;
        }

        // A handler may return a plain object (written as 200 JSON); a string is written as text, which a JSON replay would change.
        int? statusCode = result switch
        {
            null => StatusCodes.Status200OK,
            IStatusCodeHttpResult { StatusCode: { } code } => code,
            IResult => null,
            string => null,
            _ => StatusCodes.Status200OK,
        };

        if (statusCode is not { } status || status is < 200 or >= 300)
            return null;

        var value = result switch
        {
            IValueHttpResult valueResult => valueResult.Value,
            IResult => null,
            _ => result,
        };

        var jsonOptions = http.RequestServices?.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions ?? JsonSerializerOptions.Web;
        var body = value is null ? (JsonElement?)null : JsonSerializer.SerializeToElement(value, jsonOptions);

        // Only headers the endpoint added or changed; headers set earlier in the pipeline (CORS, security, correlation) belong to each request.
        var headers = http.Response.Headers
            .Where(x => headersBefore.TryGetValue(x.Key, out var before) is false || before != x.Value.ToString())
            .ToDictionary(x => x.Key, x => x.Value.Select(v => v ?? string.Empty).ToArray());

        if (result?.GetType().GetProperty("Location")?.GetValue(result) is string location)
        {
            headers["Location"] = [location];
        }

        return new IdempotencyCacheEntry(status, body, headers);
    }

    private sealed record IdempotencyCacheEntry(int StatusCode, JsonElement? Body, Dictionary<string, string[]> Headers);

    private sealed class NotCacheableResultException : Exception;

    private sealed class ReplayedResult(IdempotencyCacheEntry entry) : IResult, IStatusCodeHttpResult, IValueHttpResult
    {
        public int? StatusCode => entry.StatusCode;

        public object? Value => entry.Body;

        public Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = entry.StatusCode;

            foreach (var (name, values) in entry.Headers)
            {
                httpContext.Response.Headers[name] = values;
            }

            httpContext.Response.Headers[CustomHeaderNames.IdempotencyReplayed] = "true";

            return entry.Body is { } body
                ? httpContext.Response.WriteAsJsonAsync(body)
                : Task.CompletedTask;
        }
    }
}
