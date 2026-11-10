using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aurore.Foundation.Core.Pagination.Models;

/// <summary>
/// A list request for infinite scroll: the first call sends no cursor, and each response carries the cursor of the next call.
/// The cursor only works with the same filters and ordering it came from; the page size may change between calls.
/// Derive the request as a class with init properties (see SearchRequest) and add the filters of the query.
/// </summary>
public class CursorRequest : SearchRequest
{
    /// <summary>The cursor of the previous response. Leave it out for the first call.</summary>
    [JsonIgnore]
    public string? Cursor { get; init; }

    /// <summary>How many items to return, up to the MaxPageSize of the pagination settings.</summary>
    [JsonIgnore]
    public int? PageSize { get; init; }

    /// <summary>
    /// Identifies the filters and ordering of this request: everything but the cursor and the page size. It travels inside the
    /// cursor, so a cursor used with another query is noticed. It is a guard against mistakes, not a security measure.
    /// </summary>
    internal string Fingerprint()
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this, GetType())));

        return Convert.ToHexString(bytes, 0, 8);
    }
}
