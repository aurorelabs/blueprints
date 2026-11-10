using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Application.Models;

/// <summary>Settings of the host process itself.</summary>
[ExcludeFromCodeCoverage]
public sealed record HostingOptions
{
    /// <summary>
    /// How long the host waits for in-flight requests to finish after a stop signal before it gives up. The default matches
    /// docker stop (10 seconds). If you raise it, raise stop_grace_period in compose to at least this value plus a few seconds,
    /// or Docker kills the process first.
    /// </summary>
    public int ShutdownTimeoutSeconds { get; init; } = 10;
}
