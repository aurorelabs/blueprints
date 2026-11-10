using System.Diagnostics;
using Aurore.Foundation.Core.Application;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Core.Features.Decorators;
using Tests.Core.Features.Support;

namespace Tests.Core.Features.Decorators;

[Trait("Feature", "Feature telemetry")]
public sealed class TelemetryDecoratorTests : IDisposable
{
    private const string SourceName = "tests.features";

    private readonly List<Activity> _activities = [];
    private readonly ActivityListener _listener;

    public TelemetryDecoratorTests()
    {
        AppDiagnostics.Initialize(SourceName);

        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = _activities.Add,
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public static TheoryData<ErrorKind, ActivityStatusCode> Statuses => new()
    {
        { ErrorKind.NotFound, ActivityStatusCode.Unset },
        { ErrorKind.Conflict, ActivityStatusCode.Unset },
        { ErrorKind.Validation, ActivityStatusCode.Unset },
        { ErrorKind.Failure, ActivityStatusCode.Error },
        { ErrorKind.Unavailable, ActivityStatusCode.Error },
    };

    public void Dispose()
    {
        _listener.Dispose();
    }

    [Fact(DisplayName = "A run is a span named after the feature, tagged with its name")]
    public async Task RunIsASpanNamedAfterTheFeature()
    {
        // Arrange
        var decorator = new TelemetryDecorator();
        var context = FeatureTestHost.ContextOf<PlainFeature>(new Ping("ana"));

        // Act
        await decorator.InvokeAsync(context, () => ValueTask.FromResult(Result.Success));

        // Assert
        var span = Assert.Single(_activities);
        Assert.Equal("feature PlainFeature", span.DisplayName);
        Assert.Equal("PlainFeature", span.GetTagItem("aur.feature.name"));
        Assert.Equal(ActivityStatusCode.Unset, span.Status);
    }

    [Theory(DisplayName = "A failure adds its code to the span, and only failures on our side mark it as an error")]
    [MemberData(nameof(Statuses))]
    public async Task OnlyOurFailuresMarkTheSpanAsAnError(ErrorKind kind, ActivityStatusCode expected)
    {
        // Arrange
        var decorator = new TelemetryDecorator();
        var context = FeatureTestHost.ContextOf<PlainFeature>(new Ping("ana"));
        var error = new ApplicationError("test.error", "Something.", kind);

        // Act
        await decorator.InvokeAsync(context, () => ValueTask.FromResult<Result>(error));

        // Assert
        var span = Assert.Single(_activities);
        Assert.Equal("test.error", span.GetTagItem("aur.error.code"));
        Assert.Equal(expected, span.Status);
    }

    [Fact(DisplayName = "An exception marks the span as an error and is rethrown")]
    public async Task ExceptionMarksTheSpanAndIsRethrown()
    {
        // Arrange
        var decorator = new TelemetryDecorator();
        var context = FeatureTestHost.ContextOf<PlainFeature>(new Ping("ana"));

        // Act
        var run = async () => await decorator.InvokeAsync(context, () => throw new InvalidOperationException("boom"));

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(run);
        Assert.Equal(ActivityStatusCode.Error, Assert.Single(_activities).Status);
    }
}
