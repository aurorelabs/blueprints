namespace Aurore.Foundation.Core.Errors;

/// <summary>
/// Errors every application shares. Applications derive from this class (see AppErrors) so these are reachable from
/// the same place as their own errors: AppErrors.NotFound next to AppErrors.Customers.Duplicate.
/// </summary>
public abstract class StandardErrors
{
    /// <summary>Not meant to be created; derive from it to inherit the standard errors.</summary>
    protected StandardErrors()
    {
    }

    /// <summary>An unexpected failure on our side.</summary>
    public static readonly ApplicationError Unexpected =
        ApplicationError.Failure("general.unexpected", "An unexpected error occurred.");

    /// <summary>The input is invalid. Field details are attached with WithDetails.</summary>
    public static readonly ApplicationError Validation =
        ApplicationError.Validation("general.validation", "One or more validation errors occurred.");

    /// <summary>The requested resource does not exist.</summary>
    public static readonly ApplicationError NotFound =
        ApplicationError.NotFound("general.not_found", "The requested resource was not found.");

    /// <summary>The request conflicts with the current state.</summary>
    public static readonly ApplicationError Conflict =
        ApplicationError.Conflict("general.conflict", "The operation conflicts with the current state.");

    /// <summary>The caller is not authenticated.</summary>
    public static readonly ApplicationError Unauthorized =
        ApplicationError.Unauthorized("general.unauthorized", "Authentication is required to perform this action.");

    /// <summary>The caller is not allowed to do this.</summary>
    public static readonly ApplicationError Forbidden =
        ApplicationError.Forbidden("general.forbidden", "You do not have permission to perform this action.");
}
