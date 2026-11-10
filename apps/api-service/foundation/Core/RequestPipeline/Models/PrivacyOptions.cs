using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>Protection of personal data (client address, user agent, user id) in logs and traces.</summary>
[ExcludeFromCodeCoverage]
public sealed record PrivacyOptions
{
    /// <summary>
    /// When true, personal data is encrypted before it reaches logs and trace tags. The request context itself always
    /// holds the real values.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Extra field names to protect, added to the built-in ones (client.address, network.peer.address, user_agent.original, enduser.id).
    /// </summary>
    public string[] ProtectedFields { get; init; } = [];

    /// <summary>The encryption keys: one current key and the previous ones kept for reading old tokens.</summary>
    public PrivacyKeysOptions Keys { get; init; } = new();
}
