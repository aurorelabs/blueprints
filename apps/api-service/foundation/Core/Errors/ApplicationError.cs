using System.Collections.Generic;

namespace Aurore.Foundation.Core.Errors;

/// <summary>
/// A known, expected failure. <see cref="Code"/> is the stable key clients can rely on (for example
/// "customer.duplicate_email"), <see cref="Message"/> is the text a person reads, and <see cref="Kind"/> says what sort of
/// failure it is. <see cref="Details"/> adds per-occurrence information such as field errors (field path to messages).
/// Declared once as a static instance; <see cref="WithDetails"/> returns a copy, so the shared instance never changes.
/// </summary>
public sealed record ApplicationError
{
    private static readonly IReadOnlyDictionary<string, string[]> _noDetails = new Dictionary<string, string[]>();

    /// <summary>
    /// Creates an error. Prefer the factory for its kind, such as Conflict, and declare it once as a static field.
    /// </summary>
    public ApplicationError(string code, string message, ErrorKind kind)
    {
        Code = code;
        Message = message;
        Kind = kind;
    }

    /// <summary>
    /// The stable key clients rely on, written as "area.snake_case_name" (for example "customer.duplicate_email").
    /// </summary>
    public string Code { get; }

    /// <summary>The text a person reads. It can change without breaking clients.</summary>
    public string Message { get; }

    /// <summary>What sort of failure this is. The web layer maps it to an HTTP status.</summary>
    public ErrorKind Kind { get; }

    /// <summary>
    /// Extra information about this occurrence, such as field path to messages. Empty unless WithDetails was used.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> Details { get; init; } = _noDetails;

    /// <summary>Creates an error for invalid input.</summary>
    public static ApplicationError Validation(string code, string message)
    {
        return new(code, message, ErrorKind.Validation);
    }

    /// <summary>Creates an error for something that does not exist.</summary>
    public static ApplicationError NotFound(string code, string message)
    {
        return new(code, message, ErrorKind.NotFound);
    }

    /// <summary>Creates an error for a request that conflicts with the current state, such as a duplicate.</summary>
    public static ApplicationError Conflict(string code, string message)
    {
        return new(code, message, ErrorKind.Conflict);
    }

    /// <summary>Creates an error for a caller who is not authenticated.</summary>
    public static ApplicationError Unauthorized(string code, string message)
    {
        return new(code, message, ErrorKind.Unauthorized);
    }

    /// <summary>Creates an error for a caller who is authenticated but not allowed.</summary>
    public static ApplicationError Forbidden(string code, string message)
    {
        return new(code, message, ErrorKind.Forbidden);
    }

    /// <summary>Creates an error for something that went wrong on our side.</summary>
    public static ApplicationError Failure(string code, string message)
    {
        return new(code, message, ErrorKind.Failure);
    }

    /// <summary>Creates an error for a service or dependency that is temporarily unavailable.</summary>
    public static ApplicationError Unavailable(string code, string message)
    {
        return new(code, message, ErrorKind.Unavailable);
    }

    /// <summary>
    /// Returns a copy of this error carrying the given details (for example field path to messages).
    /// </summary>
    public ApplicationError WithDetails(IReadOnlyDictionary<string, string[]> details)
    {
        return this with { Details = details };
    }
}
