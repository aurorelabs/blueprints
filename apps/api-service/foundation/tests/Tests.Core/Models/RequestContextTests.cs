using System.Diagnostics;
using Aurore.Foundation.Core.Models;

namespace Tests.Core.Models;

[Trait("Feature", "Request context")]
public sealed class RequestContextTests
{
    [Fact(DisplayName = "Initialize stores each value in its own property")]
    public void InitializeStoresTheValues()
    {
        // Arrange
        var context = new RequestContext();

        // Act
        context.Initialize("corr-1", "trace-1", "span-1", "idem-1", "203.0.113.7", "agent/1.0", "example.com");

        // Assert
        Assert.Equal("corr-1", context.CorrelationId);
        Assert.Equal("trace-1", context.TraceId);
        Assert.Equal("span-1", context.OriginalSpanId);
        Assert.Equal("idem-1", context.IdempotencyKey);
        Assert.Equal("203.0.113.7", context.ClientAddress);
        Assert.Equal("agent/1.0", context.UserAgent);
        Assert.Equal("example.com", context.Host);
        Assert.Null(context.UserId);
    }

    [Fact(DisplayName = "The context can only be initialized once, so nothing can overwrite the correlation id")]
    public void CannotBeInitializedTwice()
    {
        // Arrange
        var context = Create();

        // Act
        var exception = Record.Exception(() => context.Initialize("other", "t", null, null, null, null, null));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("corr-1", context.CorrelationId);
    }

    [Fact(DisplayName = "The user id can be set once, after authentication")]
    public void UserIdIsSetOnce()
    {
        // Arrange
        var context = Create();
        context.SetUserId("user-1");

        // Act
        var exception = Record.Exception(() => context.SetUserId("user-2"));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Equal("user-1", context.UserId);
    }

    [Fact(DisplayName = "Without an active span the current span falls back to the span of the request")]
    public void SpanIdFallsBackToTheRequestSpan()
    {
        // Arrange
        var context = Create();
        var previous = Activity.Current;
        Activity.Current = null;

        try
        {
            // Act
            var spanId = context.SpanId;

            // Assert
            Assert.Equal("span-1", spanId);
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    private static RequestContext Create()
    {
        var context = new RequestContext();
        context.Initialize("corr-1", "trace-1", "span-1", "idem-1", "203.0.113.7", "agent/1.0", "example.com");

        return context;
    }
}
