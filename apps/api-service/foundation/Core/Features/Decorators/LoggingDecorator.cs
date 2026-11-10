using System.Diagnostics;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Results;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.Core.Features.Decorators;

/// <summary>
/// Writes one line per run with the feature, the outcome and the time it took. The request is never logged: it can carry
/// personal data.
/// </summary>
public sealed partial class LoggingDecorator(ILogger<LoggingDecorator> logger) : IFeatureDecorator
{
    /// <inheritdoc />
    public int Order => 200;

    /// <inheritdoc />
    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        var started = Stopwatch.GetTimestamp();
        var result = await next();
        var elapsed = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (result.IsSuccess)
            LogSucceeded(context.FeatureName, elapsed);
        else if (result.Error!.Kind is ErrorKind.Failure or ErrorKind.Unavailable)
            LogFailedOnOurSide(context.FeatureName, result.Error.Code, elapsed);
        else
            LogFailed(context.FeatureName, result.Error.Code, elapsed);

        return result;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Feature {Feature} succeeded in {ElapsedMs} ms")]
    private partial void LogSucceeded(string feature, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Feature {Feature} answered {ErrorCode} in {ElapsedMs} ms")]
    private partial void LogFailed(string feature, string errorCode, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Feature {Feature} failed with {ErrorCode} in {ElapsedMs} ms")]
    private partial void LogFailedOnOurSide(string feature, string errorCode, long elapsedMs);
}
