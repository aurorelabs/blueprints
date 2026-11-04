using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Aurore.Foundation.Core.Application;

public static class AppDiagnostics
{
    private static readonly Lock _lock = new();
    private static ActivitySource? _source;

    public static ActivitySource Source
    {
        get
        {
            return _source ?? throw new InvalidOperationException(
                $"{nameof(AppDiagnostics)} is not initialized. Call {nameof(Initialize)} at startup.");
        }
    }

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

    public static Activity? StartActivity(this string name, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        return _source?.StartActivity(name, ActivityKind.Internal, parentContext: default, tags);
    }
}
