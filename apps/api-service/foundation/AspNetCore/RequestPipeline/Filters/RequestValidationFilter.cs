using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.Core.Validation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Filters;

/// <summary>
/// Validates the arguments of every endpoint with their request validators (IRequestValidator) before the handler runs,
/// and answers an invalid request with the 400 validation problem, so handlers never validate by hand. Applied globally
/// by MapAuroreMinimalEndpoints.
///
/// Which parameters are validated is decided once, when the endpoint is built: a parameter is validated when a validator
/// is registered for its declared type as an IRequestValidator. Endpoints with no such parameter get no filter at all, so
/// they pay nothing per request, and primitives, services and other framework arguments are never looked at.
///
/// Only IRequestValidator validators run here. A business rule, which can fail as another kind of error (for example a 409),
/// uses IFeatureValidator and is run by the feature pipeline.
/// </summary>
internal static class RequestValidationFilter
{
    private static readonly MethodInfo _validateMethod = typeof(RequestValidationFilter)
        .GetMethod(nameof(ValidateAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private delegate Task<List<ValidationFailure>> ValidateArgument(IServiceProvider services, object argument, CancellationToken cancellationToken);

    /// <summary>
    /// The endpoint filter factory: returns the next delegate untouched when no parameter has a validator.
    /// </summary>
    internal static EndpointFilterDelegate Create(EndpointFilterFactoryContext factory, EndpointFilterDelegate next)
    {
        var isService = factory.ApplicationServices.GetRequiredService<IServiceProviderIsService>();
        var parameters = factory.MethodInfo.GetParameters();
        var targets = new List<(int Index, ValidateArgument Validate)>();

        for (var index = 0; index < parameters.Length; index++)
        {
            var type = parameters[index].ParameterType;

            if (type.IsByRef || type.IsPointer || type.IsByRefLike)
                continue;

            if (isService.IsService(typeof(IRequestValidator<>).MakeGenericType(type)) is false)
                continue;

            targets.Add((index, _validateMethod.MakeGenericMethod(type).CreateDelegate<ValidateArgument>()));
        }

        if (targets.Count == 0)
            return next;

        return async context =>
        {
            List<ValidationFailure>? failures = null;

            foreach (var (index, validate) in targets)
            {
                if (context.Arguments[index] is not { } argument)
                    continue;

                var found = await validate(context.HttpContext.RequestServices, argument, context.HttpContext.RequestAborted);

                if (found.Count > 0)
                {
                    failures ??= [];
                    failures.AddRange(found);
                }
            }

            return failures is null
                ? await next(context)
                : failures.ToApplicationError().ToHttpResult();
        };
    }

    /// <summary>
    /// Runs every validator registered for T, so a second validator for the same type is never ignored.
    /// </summary>
    private static async Task<List<ValidationFailure>> ValidateAsync<T>(IServiceProvider services, object argument, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        foreach (var validator in services.GetServices<IRequestValidator<T>>())
        {
            var result = await validator.ValidateAsync((T)argument, cancellationToken);
            failures.AddRange(result.Errors);
        }

        return failures;
    }
}
