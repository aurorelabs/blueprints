using System.Collections.Generic;

namespace Aurore.Foundation.Core.Authentication.Models;

/// <summary>
/// API key authentication, accepted alongside JWT Bearer. Disabled when there are no clients.
/// Keys are secrets: keep only local values in appsettings.json and supply real ones through environment variables or a
/// secret store (e.g. ApiKeys__Clients__billing-service__Key).
/// </summary>
public sealed record ApiKeysOptions
{
    public string HeaderName { get; init; } = "AL-Api-Key";

    /// <summary>
    /// Client name -&gt; settings. The client name becomes the caller's identity (name and NameIdentifier claims).
    /// </summary>
    public Dictionary<string, ApiKeyClientOptions> Clients { get; init; } = [];
}
