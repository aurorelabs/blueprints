using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Aurore.Foundation.Core.Constants;
using Aurore.Foundation.Core.RequestPipeline.Models;

namespace Aurore.Foundation.Core.Privacy;

/// <summary>
/// AES-256-GCM with a random nonce: the same input never produces the same token twice. A token is
/// base64url(nonce + ciphertext + tag) and says nothing about the key. Encryption uses the current key; decryption tries
/// the current key and then each previous key, which is safe because a wrong key fails the authentication tag.
/// </summary>
public sealed class PrivacyProtector : IPrivacyProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static readonly string[] _defaultFields =
    [
        OpenTelemetryProperties.Attributes.Standard.ClientAddress,
        OpenTelemetryProperties.Attributes.Standard.NetworkPeerAddress,
        OpenTelemetryProperties.Attributes.Standard.UserAgentOriginal,
        OpenTelemetryProperties.Attributes.Standard.EndUserId,
    ];

    private readonly bool _enabled;
    private readonly HashSet<string> _fields;
    private readonly byte[][] _keys;

    /// <summary>
    /// Creates the protector. With privacy enabled the keys must be valid; the options validator checks them at startup.
    /// </summary>
    public PrivacyProtector(PrivacyOptions options)
    {
        _enabled = options.Enabled;
        _fields = new HashSet<string>([.. _defaultFields, .. options.ProtectedFields], StringComparer.OrdinalIgnoreCase);
        _keys = _enabled
            ? [.. new[] { options.Keys.Current }.Concat(options.Keys.Previous).Select(Convert.FromBase64String)]
            : [];
    }

    /// <summary>True when privacy is on and the field is one of the protected fields.</summary>
    public bool IsProtected(string field)
    {
        return _enabled && _fields.Contains(field);
    }

    /// <summary>Encrypts the value when its field is protected; otherwise returns it unchanged.</summary>
    public string? Protect(string field, string? value)
    {
        if (string.IsNullOrEmpty(value) || IsProtected(field) is false)
            return value;

        var plaintext = Encoding.UTF8.GetBytes(value);
        var payload = new byte[NonceSize + plaintext.Length + TagSize];
        var nonce = payload.AsSpan(0, NonceSize);
        var ciphertext = payload.AsSpan(NonceSize, plaintext.Length);
        var tag = payload.AsSpan(NonceSize + plaintext.Length, TagSize);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_keys[0], TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return Base64Url(payload);
    }

    /// <summary>
    /// Decrypts a token with the current key, then each previous key. Returns null when none can read it.
    /// </summary>
    public string? Unprotect(string token)
    {
        byte[] payload;

        try
        {
            payload = FromBase64Url(token);
        }
        catch (FormatException)
        {
            return null;
        }

        if (payload.Length < NonceSize + TagSize)
            return null;

        var nonce = payload.AsSpan(0, NonceSize);
        var ciphertext = payload.AsSpan(NonceSize, payload.Length - NonceSize - TagSize);
        var tag = payload.AsSpan(payload.Length - TagSize, TagSize);
        var plaintext = new byte[ciphertext.Length];

        foreach (var key in _keys)
        {
            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, ciphertext, tag, plaintext);

                return Encoding.UTF8.GetString(plaintext);
            }
            catch (CryptographicException)
            {
                // Wrong key: try the next one.
            }
        }

        return null;
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] FromBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');

        return Convert.FromBase64String(base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '='));
    }
}
