using Aurore.Foundation.Core.Errors;

namespace Aurore.Blueprints.ApiService.Core.Errors;

/// <summary>
/// The single place for this application's errors. The standard ones (AppErrors.NotFound, AppErrors.Conflict, ...) are
/// inherited from the foundation; business errors are grouped in nested classes, one line each:
///
///   public static class Customers
///   {
///       public static readonly ApplicationError Duplicate =
///           ApplicationError.Conflict("customer.duplicate_email", "A customer with this email already exists.");
///   }
///
/// Codes read "area.snake_case_name" and are what clients rely on; messages can change freely.
/// </summary>
public sealed class AppErrors : StandardErrors
{
    private AppErrors()
    {
    }
}
