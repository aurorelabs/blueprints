namespace Aurore.Foundation.Core.Errors;

/// <summary>
/// What sort of failure an <see cref="ApplicationError"/> is. The web layer decides what each kind means as an HTTP status.
/// </summary>
public enum ErrorKind
{
    /// <summary>The input is invalid.</summary>
    Validation,

    /// <summary>The requested thing does not exist.</summary>
    NotFound,

    /// <summary>The request conflicts with the current state (duplicate, stale data, wrong state).</summary>
    Conflict,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,

    /// <summary>The caller is authenticated but not allowed.</summary>
    Forbidden,

    /// <summary>Something went wrong on our side.</summary>
    Failure,

    /// <summary>The service or a dependency is temporarily unavailable.</summary>
    Unavailable,
}
