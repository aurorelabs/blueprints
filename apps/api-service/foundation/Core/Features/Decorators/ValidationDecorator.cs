using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Core.Validation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Core.Features.Decorators;

/// <summary>
/// Runs the business validators (IFeatureValidator) of the request before the feature and answers with their failure, so the
/// feature only sees requests that passed. A request with no validator goes straight through. The validators of the request's
/// shape (IRequestValidator) are not run here: the HTTP validation filter owns them.
/// </summary>
public sealed class ValidationDecorator : IFeatureDecorator
{
    private static readonly MethodInfo _validate = typeof(ValidationDecorator)
        .GetMethod(nameof(ValidateAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<Outcome>>?> _validators = new();

    /// <inheritdoc />
    public int Order => 300;

    /// <inheritdoc />
    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        var validate = _validators.GetOrAdd(context.RequestType, type => Build(type, context.Services));

        if (validate is null)
            return await next();

        var outcome = await validate(context.Services, context.Request, context.CancellationToken);

        return outcome.Failures.Count == 0
            ? await next()
            : outcome.Failures.ToApplicationError(outcome.Error);
    }

    private static Func<IServiceProvider, object, CancellationToken, Task<Outcome>>? Build(Type requestType, IServiceProvider services)
    {
        var isService = services.GetRequiredService<IServiceProviderIsService>();

        return isService.IsService(typeof(IFeatureValidator<>).MakeGenericType(requestType))
            ? _validate.MakeGenericMethod(requestType).CreateDelegate<Func<IServiceProvider, object, CancellationToken, Task<Outcome>>>()
            : null;
    }

    private static async Task<Outcome> ValidateAsync<T>(IServiceProvider services, object request, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        ApplicationError? error = null;

        foreach (var validator in services.GetServices<IFeatureValidator<T>>())
        {
            var result = await validator.ValidateAsync((T)request, cancellationToken);

            if (result.IsValid)
                continue;

            failures.AddRange(result.Errors);
            error ??= (validator as FeatureValidator<T>)?.Error;
        }

        return new Outcome(failures, error);
    }

    private sealed record Outcome(List<ValidationFailure> Failures, ApplicationError? Error);
}
