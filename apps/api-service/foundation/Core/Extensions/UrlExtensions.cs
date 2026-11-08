using System;

namespace Aurore.Foundation.Core.Extensions;

public static class UrlExtensions
{
    public static bool IsAbsoluteUrl(this string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    /// <summary>
    /// True for loopback addresses and *.localhost hosts: addresses that only make sense on a developer machine.
    /// </summary>
    public static bool IsLocalUrl(this string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.IsLoopback || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsHttpsUrl(this string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }
}
