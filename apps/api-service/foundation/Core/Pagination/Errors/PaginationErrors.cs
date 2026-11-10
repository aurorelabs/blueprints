using Aurore.Foundation.Core.Errors;

namespace Aurore.Foundation.Core.Pagination.Errors;

/// <summary>The errors of paged requests. The field details say which parameter was wrong.</summary>
public static class PaginationErrors
{
    /// <summary>A paging, ordering or date range parameter is invalid.</summary>
    public static readonly ApplicationError InvalidRequest =
        ApplicationError.Validation("pagination.invalid_request", "The paging, ordering or filtering parameters are invalid.");

    /// <summary>The cursor is not one this service wrote, or does not fit the ordering.</summary>
    public static readonly ApplicationError InvalidCursor =
        ApplicationError.Validation("pagination.invalid_cursor", "The cursor is invalid. Send the nextCursor of a previous response unchanged.");

    /// <summary>The cursor came from a request with other filters or ordering.</summary>
    public static readonly ApplicationError CursorMismatch =
        ApplicationError.Validation("pagination.cursor_mismatch", "The cursor belongs to a request with other filters or ordering. Start again without a cursor.");
}
