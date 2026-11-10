using System.Security.Cryptography;
using Aurore.Foundation.Core.Privacy;
using Aurore.Foundation.Core.RequestPipeline.Models;

namespace Tests.Shared.Privacy;

/// <summary>
/// Builders for privacy settings and protectors, so tests never repeat key generation.
/// </summary>
public static class PrivacyTestData
{
    /// <summary>
    /// A new random 32-byte key, base64 encoded, like the ones used in production.
    /// </summary>
    public static string NewKey()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    /// <summary>
    /// Settings with privacy on. Without arguments a fresh random key is used.
    /// </summary>
    public static PrivacyOptions Enabled(string? currentKey = null, params string[] previousKeys)
    {
        return new PrivacyOptions
        {
            Enabled = true,
            Keys = new() { Current = currentKey ?? NewKey(), Previous = previousKeys },
        };
    }

    /// <summary>
    /// A protector with privacy on. Without arguments a fresh random key is used.
    /// </summary>
    public static PrivacyProtector Protector(string? currentKey = null, params string[] previousKeys)
    {
        return new PrivacyProtector(Enabled(currentKey, previousKeys));
    }

    /// <summary>
    /// A protector with privacy off: every value passes through unchanged.
    /// </summary>
    public static PrivacyProtector DisabledProtector()
    {
        return new PrivacyProtector(new PrivacyOptions());
    }
}
