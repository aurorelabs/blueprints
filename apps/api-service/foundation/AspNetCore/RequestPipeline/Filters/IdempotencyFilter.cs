using System;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Constants;
using Aurore.Foundation.Core.Models;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Filters;

public sealed class IdempotencyFilter(HybridCache cache, IdempotencyOptions options, RequestContext requestContext) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var key = requestContext.IdempotencyKey;

        if (key is null)
            return await next(context);

        var request = context.HttpContext.Request;
        var cacheKey = $"{CustomHeaderNames.IdempotencyKey}:{request.Method}:{request.Path}:{key}";

        try
        {
            var entry = await cache.GetOrCreateAsync(
                cacheKey,
                (context, next),
                static async (state, _) =>
                {
                    var result = await state.next(state.context);

                    // Only 2xx results are cached; throwing keeps GetOrCreateAsync from storing anything.
                    if (result is not IStatusCodeHttpResult { StatusCode: { } statusCode } || statusCode is < 200 or >= 300)
                        throw new NotCacheableResultException(result);

                    return new IdempotencyCacheEntry(statusCode, (result as IValueHttpResult)?.Value);
                },
                new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(options.CacheDurationMinutes) },
                cancellationToken: context.HttpContext.RequestAborted);

            return entry.Value is null
                ? Results.StatusCode(entry.StatusCode)
                : Results.Json(entry.Value, statusCode: entry.StatusCode);
        }
        catch (NotCacheableResultException ex)
        {
            return ex.Result;
        }
    }

    private sealed record IdempotencyCacheEntry(int StatusCode, object? Value);

    private sealed class NotCacheableResultException(object? result) : Exception
    {
        public object? Result { get; } = result;
    }
}
