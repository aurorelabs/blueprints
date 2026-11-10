using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Core.Validation;
using FluentValidation;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Results;

internal sealed record DemoOrder(int Id, string Email, int Quantity, string Status);

internal sealed record CreateDemoOrderRequest(string Email, int Quantity);

internal sealed class CreateDemoOrderValidator : RequestValidator<CreateDemoOrderRequest>
{
    public CreateDemoOrderValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
    }
}

/// <summary>
/// A business rule written with FluentValidation as a feature validator: only pending orders can be cancelled. It is never
/// run by the HTTP validation filter, only by the feature (here, DemoOrders).
/// </summary>
internal sealed class CancelDemoOrderValidator : FeatureValidator<DemoOrder>
{
    public CancelDemoOrderValidator()
    {
        RuleFor(x => x.Status).Equal("pending").WithMessage("Only pending orders can be cancelled.");
    }
}

/// <summary>
/// A stand-in for a feature: in-memory data and methods that return results. Order 1 is pending, order 2 is shipped.
/// </summary>
internal static class DemoOrders
{
    private static readonly ConcurrentDictionary<int, DemoOrder> _orders = new()
    {
        [1] = new DemoOrder(1, "ada@example.com", 2, "pending"),
        [2] = new DemoOrder(2, "grace@example.com", 1, "shipped"),
    };

    private static int _lastId = 2;

    public static Result<DemoOrder> Get(int id)
    {
        return _orders.TryGetValue(id, out var order) ? order : DemoErrors.Orders.NotFound;
    }

    public static Result<DemoOrder> Create(CreateDemoOrderRequest request)
    {
        if (_orders.Values.Any(x => x.Email == request.Email))
            return DemoErrors.Orders.DuplicateEmail;

        var order = new DemoOrder(Interlocked.Increment(ref _lastId), request.Email, request.Quantity, "pending");
        _orders[order.Id] = order;

        return order;
    }

    public static Result<DemoOrder> Cancel(int id)
    {
        var found = Get(id);

        if (found.IsFailure)
            return found.Error!;

        var validation = new CancelDemoOrderValidator().Validate(found.Value);

        if (validation.IsValid is false)
            return validation.ToApplicationError(DemoErrors.Orders.CannotCancel);

        var cancelled = found.Value with { Status = "cancelled" };
        _orders[id] = cancelled;

        return cancelled;
    }

    public static Result Delete(int id)
    {
        return _orders.TryRemove(id, out _) ? Result.Success : DemoErrors.Orders.NotFound;
    }
}
