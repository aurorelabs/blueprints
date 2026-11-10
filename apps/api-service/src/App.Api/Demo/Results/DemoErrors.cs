using Aurore.Foundation.Core.Errors;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Results;

/// <summary>
/// The demo's version of AppErrors: standard errors are inherited (DemoErrors.NotFound), business errors are grouped in
/// nested classes (DemoErrors.Orders.DuplicateEmail). A real application keeps this in one place, in its Core project.
/// </summary>
internal sealed class DemoErrors : StandardErrors
{
    private DemoErrors()
    {
    }

    public static class Orders
    {
        public static readonly ApplicationError NotFound =
            ApplicationError.NotFound("order.not_found", "The order was not found.");

        public static readonly ApplicationError DuplicateEmail =
            ApplicationError.Conflict("order.duplicate_email", "An order for this email already exists.");

        public static readonly ApplicationError CannotCancel =
            ApplicationError.Conflict("order.cannot_cancel", "The order can no longer be cancelled.");
    }
}
