using System;

namespace Aurore.Foundation.Core.Extensions;

/// <summary>URL checks.</summary>
public static class UrlExtensions
{
    /// <summary>True when the text is an absolute URL.</summary>
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

    /// <summary>True when the text is an absolute URL using HTTPS.</summary>
    public static bool IsHttpsUrl(this string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
    }
}
