# Feature decorators

A feature is a use case: one class, one request, one result. It derives from `Feature<TRequest>` (a command that
returns a plain `Result`) or `Feature<TRequest, TResponse>` (returns `Result<TResponse>`), and the code goes in
`ExecuteAsync`. Endpoints, message handlers and background jobs inject the feature class itself and call
`HandleAsync`.

Decorators wrap `ExecuteAsync`. **Nothing is on unless it is asked for**, so the same features can be registered with
another set of decorators in another host.

## Enabling decorators

For all features of a registration:

```csharp
services.AddFeaturesFrom<IFeatures>(features => features
    .WithTelemetry()
    .WithLogging()
    .WithValidation()
    .With<MyDecorator>());     // a decorator of your own
```

For one feature, with attributes (the attribute wins over the registration; `Without` wins over `With`):

```csharp
[WithLogging]                  // add one the registration did not enable
[Without<TelemetryDecorator>]  // remove one the registration enabled
public sealed class SearchCustomers(...) : Feature<SearchCustomersRequest, Page<CustomerItem>>
```

**The order of the `With` calls and of the attributes does not decide the order the decorators run in.** Each decorator has
an `Order` (lower runs first, outermost): telemetry is 100, logging 200, validation 300, and a custom decorator defaults to
1000, right before the feature. Override `Order` in your decorator to place it elsewhere. The one case where the order of
the calls matters is two decorators with the same `Order`: they run in the order they were enabled, the registration's
first and then the feature's attributes.

## Included

| Decorator | What it does |
|---|---|
| Telemetry | A span `feature {Name}` with `aur.feature.name`. A failed result adds `aur.error.code`; only `Failure` and `Unavailable` mark the span as an error (a not found or a conflict is a normal answer). |
| Logging | One line per run with the feature, the outcome and the time. `Warning` for failures on our side, `Information` otherwise. The request is never logged: it can carry personal data. |
| Validation | Runs the `IFeatureValidator<TRequest>` validators of the request (business rules) before the feature. A request with none goes straight through. A validator can declare its error (`Error => AppErrors.Customers.Duplicate`) to answer 409 instead of 400. The `IRequestValidator<T>` validators (shape) belong to the HTTP filter and are not run here. |

## Writing your own

```csharp
public sealed class AuditDecorator(IAuditLog audit) : IFeatureDecorator
{
    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        var result = await next();
        audit.Record(context.FeatureName, result.IsSuccess);

        return result;
    }
}
```

Enable it with `.With<AuditDecorator>()` or `[With<AuditDecorator>]`. It is resolved from the container, so it can take
services. Returning a failed `Result` without calling `next` stops the feature; the failure comes back as the feature's own
result type.

## Ideas for later

These are not implemented. Each would be one decorator.

- **Transaction.** Begin before, commit on success, roll back on a failed `Result` (not only on an exception). It only
  pays off when a feature saves more than once or must undo a partly done write: one `SaveChanges` is already atomic.
- **Exception mapping.** Turn a unique-constraint violation into a Conflict result, so two simultaneous requests get a 409
  instead of a 500 (the database constraint is the real guarantee). Belongs in the EF Core foundation.
- **Caching.** Cache the result of a query feature by request, through a marker or an attribute.
- **Feature-level authorization.** "Can this user act on this record": a business rule on the current user and the
  resource, not the role/scope check of the endpoint.
- **Retry.** Re-run on a concurrency conflict (`DbUpdateConcurrencyException`).
- **Body logging.** Only opt-in per request type, at debug level, through a redactor that masks sensitive properties.
