using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.RequestPipeline.Models;

/// <summary>The secrets used to encrypt personal data.</summary>
[ExcludeFromCodeCoverage]
public sealed record PrivacyKeysOptions
{
    /// <summary>
    /// Encrypts every new value. 32 random bytes, base64 encoded. Set it through an environment variable, never commit it.
    /// </summary>
    public string Current { get; init; } = string.Empty;

    /// <summary>
    /// Old secrets, kept only to read tokens written before a rotation. Remove them to make everything they protected unreadable.
    /// </summary>
    public string[] Previous { get; init; } = [];
}
