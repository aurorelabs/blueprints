using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Aurore.Foundation.Core.Pagination.Services;

/// <summary>
/// Writes and reads the cursor: base64url of a small JSON document with the fingerprint of the query and the sort values of
/// the last item. It is not encrypted; it is only meant to be passed back as received.
/// </summary>
internal static class CursorCodec
{
    internal sealed record Decoded(string Fingerprint, JsonElement[] Values);

    internal static string Encode(string fingerprint, JsonElement[] values)
    {
        var json = JsonSerializer.Serialize(new { h = fingerprint, v = values });

        return Base64Url.EncodeToString(System.Text.Encoding.UTF8.GetBytes(json));
    }

    /// <summary>Reads a cursor, or returns null when the text is not one this service wrote.</summary>
    internal static Decoded? Decode(string cursor)
    {
        try
        {
            using var document = JsonDocument.Parse(Base64Url.DecodeFromChars(cursor));
            var root = document.RootElement;

            return new Decoded(
                root.GetProperty("h").GetString() ?? string.Empty,
                [.. root.GetProperty("v").EnumerateArray().Select(x => x.Clone())]);
        }
        catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return null;
        }
    }
}
