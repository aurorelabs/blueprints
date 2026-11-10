using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>Settings of response compression.</summary>
[ExcludeFromCodeCoverage]
public sealed record ResponseCompressionConfigurationOptions
{
    /// <summary>
    /// Turn off when a reverse proxy already compresses responses.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Fine for bearer-token APIs; BREACH/CRIME mostly matter for cookie auth mixed with attacker-controlled input.
    /// </summary>
    public bool EnableForHttps { get; init; } = true;
}
