// Privacy helper for the encrypted values in logs and traces. Standalone: needs only the .NET 10 SDK.
//
//   dotnet run docs/privacy.cs -- generate-key
//       Prints a new random 32-byte secret (base64) to use as RequestPipeline__Privacy__Keys__Current.
//
//   dotnet run docs/privacy.cs -- unprotect <token>
//       Decrypts a token found in a log line or span tag.
//       Reads the keys from PRIVACY_KEYS, a comma-separated list of secrets: the current one and every previous one you still keep.
//       Each key is tried in turn.
//
// Token format (same as the service): base64url(nonce[12] + ciphertext + tag[16]), AES-256-GCM.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

const int NonceSize = 12;
const int TagSize = 16;

return args switch
{
    ["generate-key"] => GenerateKey(),
    ["unprotect", var token] => Unprotect(token),
    _ => Usage(),
};

static int Usage()
{
    Console.Error.WriteLine("Usage: dotnet run docs/privacy.cs -- generate-key | unprotect <token>");

    return 2;
}

static int GenerateKey()
{
    Console.WriteLine(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

    return 0;
}

static int Unprotect(string token)
{
    var keys = (Environment.GetEnvironmentVariable("PRIVACY_KEYS") ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(Convert.FromBase64String)
        .ToArray();

    if (keys.Length == 0)
    {
        Console.Error.WriteLine("Set PRIVACY_KEYS to a comma-separated list of secrets (current first, then previous ones).");

        return 1;
    }

    try
    {
        var base64 = token.Replace('-', '+').Replace('_', '/');
        var payload = Convert.FromBase64String(base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '='));
        var plaintext = new byte[payload.Length - NonceSize - TagSize];

        foreach (var key in keys)
        {
            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(
                    payload.AsSpan(0, NonceSize),
                    payload.AsSpan(NonceSize, plaintext.Length),
                    payload.AsSpan(payload.Length - TagSize, TagSize),
                    plaintext);

                Console.WriteLine(Encoding.UTF8.GetString(plaintext));

                return 0;
            }
            catch (CryptographicException)
            {
                // Wrong key: try the next one.
            }
        }
    }
    catch (Exception ex) when (ex is FormatException or ArgumentException)
    {
        Console.Error.WriteLine("The token is malformed.");

        return 1;
    }

    Console.Error.WriteLine("None of the keys could decrypt this token (wrong key, or the token was altered).");

    return 1;
}
