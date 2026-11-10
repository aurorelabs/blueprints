using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Authentication.Models;

/// <summary>One client that may call the service with an API key.</summary>
[ExcludeFromCodeCoverage]
public sealed record ApiKeyClientOptions
{
    /// <summary>The secret key the client sends. Supply it through an environment variable or a secret store.</summary>
    public string Key { get; init; } = string.Empty;
    /// <summary>The roles granted to this client.</summary>
    public string[] Roles { get; init; } = [];
    /// <summary>The scopes granted to this client.</summary>
    public string[] Scopes { get; init; } = [];
}
