using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Aurore.Foundation.Core.Application;

/// <summary>
/// The application's shared ActivitySource. Initialize it once at startup so every part of the app creates its spans
/// under the same source name.
/// </summary>
public static class AppDiagnostics
{
    private static readonly Lock _lock = new();
    private static ActivitySource? _source;

    /// <summary>The shared source. Throws when Initialize has not been called yet.</summary>
    public static ActivitySource Source
    {
        get
        {
            return _source ?? throw new InvalidOperationException(
                $"{nameof(AppDiagnostics)} is not initialized. Call {nameof(Initialize)} at startup.");
        }
    }

    /// <summary>
    /// Creates the shared source. Calling it again with the same name is harmless; a different name throws.
    /// </summary>
    public static void Initialize(string sourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        lock (_lock)
        {
            if (_source is null)
            {
                _source = new ActivitySource(sourceName);
                return;
            }

            if (_source.Name != sourceName)
                throw new InvalidOperationException($"{nameof(AppDiagnostics)} is already initialized with source '{_source.Name}'.");
        }
    }

    /// <summary>
    /// Starts an internal span with this name on the shared source. Returns null before Initialize or when nothing
    /// listens to the source.
    /// </summary>
    public static Activity? StartActivity(this string name, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        return _source?.StartActivity(name, ActivityKind.Internal, parentContext: default, tags);
    }
}
