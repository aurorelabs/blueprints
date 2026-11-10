using Microsoft.Extensions.Logging;

namespace Tests.Shared.Logging;

/// <summary>
/// A logger that keeps what was put in its scopes and what was logged, so tests can check what a component would have
/// written to the logs.
/// </summary>
public sealed class CapturingLogger<T> : ILogger<T>
{
    /// <summary>
    /// Every key and value of every scope opened on this logger. A later scope overwrites an earlier key.
    /// </summary>
    public Dictionary<string, object?> Scope { get; } = [];

    /// <summary>Every line logged, with its level and its formatted message.</summary>
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            foreach (var (key, value) in pairs)
            {
                Scope[key] = value;
            }
        }

        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, formatter(state, exception)));
    }
}
