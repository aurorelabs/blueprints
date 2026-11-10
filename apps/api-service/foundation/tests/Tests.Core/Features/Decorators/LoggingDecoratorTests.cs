using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Core.Features.Decorators;
using Microsoft.Extensions.Logging;
using Tests.Core.Features.Support;
using Tests.Shared.Logging;

namespace Tests.Core.Features.Decorators;

[Trait("Feature", "Feature logging")]
public sealed class LoggingDecoratorTests
{
    public static TheoryData<ErrorKind, LogLevel> Levels => new()
    {
        { ErrorKind.NotFound, LogLevel.Information },
        { ErrorKind.Conflict, LogLevel.Information },
        { ErrorKind.Validation, LogLevel.Information },
        { ErrorKind.Failure, LogLevel.Warning },
        { ErrorKind.Unavailable, LogLevel.Warning },
    };

    [Fact(DisplayName = "A successful run logs the feature and the time it took")]
    public async Task SuccessLogsTheFeatureAndTheTime()
    {
        // Arrange
        var logger = new CapturingLogger<LoggingDecorator>();
        var decorator = new LoggingDecorator(logger);
        var context = FeatureTestHost.ContextOf<PlainFeature>(new Ping("ana"));

        // Act
        await decorator.InvokeAsync(context, () => ValueTask.FromResult(Result.Success));

        // Assert
        var (level, message) = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, level);
        Assert.StartsWith("Feature PlainFeature succeeded in ", message);
    }

    [Theory(DisplayName = "A failure logs its code, as a warning only when the failure is on our side")]
    [MemberData(nameof(Levels))]
    public async Task FailureLogsItsCodeAtTheRightLevel(ErrorKind kind, LogLevel expected)
    {
        // Arrange
        var logger = new CapturingLogger<LoggingDecorator>();
        var decorator = new LoggingDecorator(logger);
        var context = FeatureTestHost.ContextOf<PlainFeature>(new Ping("ana"));
        var error = new ApplicationError("test.error", "Something.", kind);

        // Act
        await decorator.InvokeAsync(context, () => ValueTask.FromResult<Result>(error));

        // Assert
        var (level, message) = Assert.Single(logger.Entries);
        Assert.Equal(expected, level);
        Assert.Contains("test.error", message);
    }

    [Fact(DisplayName = "The request is never written to the log, since it can carry personal data")]
    public async Task RequestIsNeverLogged()
    {
        // Arrange
        var logger = new CapturingLogger<LoggingDecorator>();
        var decorator = new LoggingDecorator(logger);
        var context = FeatureTestHost.ContextOf<PlainFeature>(new Ping("secret-name"));

        // Act
        await decorator.InvokeAsync(context, () => ValueTask.FromResult(Result.Success));

        // Assert
        Assert.DoesNotContain(logger.Entries, x => x.Message.Contains("secret-name"));
    }
}
