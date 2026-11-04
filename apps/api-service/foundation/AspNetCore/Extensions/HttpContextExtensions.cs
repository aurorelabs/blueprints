using Microsoft.AspNetCore.Http;

namespace Aurore.Foundation.AspNetCore.Extensions;

public static class HttpContextExtensions
{
    public static string GetRequestHeader(this HttpContext context, string key, string? fallbackValue = null)
    {
        return context.Request.Headers.TryGetValue(key, out var headerValue) is not true
            ? fallbackValue ?? string.Empty
            : headerValue.ToString();
    }
}
