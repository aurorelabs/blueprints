using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Application;
using Aurore.Foundation.Core.Constants;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Results;

namespace Aurore.Foundation.Core.Features.Decorators;

/// <summary>
/// Runs the feature inside a span named after it. A failed result adds its error code; only failures on our side
/// (Failure, Unavailable) mark the span as an error, since a not found or a conflict is a normal answer.
/// </summary>
public sealed class TelemetryDecorator : IFeatureDecorator
{
    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        using var activity = $"feature {context.FeatureName}".StartActivity(
            [new(OpenTelemetryProperties.Attributes.Custom.FeatureName, context.FeatureName)]);

        try
        {
            var result = await next();

            if (activity is not null && result.IsFailure)
            {
                activity.SetTag(OpenTelemetryProperties.Attributes.Custom.ErrorCode, result.Error!.Code);

                if (result.Error.Kind is ErrorKind.Failure or ErrorKind.Unavailable)
                    activity.SetStatus(ActivityStatusCode.Error, result.Error.Code);
            }

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.GetType().Name);

            throw;
        }
    }
}
