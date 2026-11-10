using System.Net;
using System.Text.RegularExpressions;
using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.Models;
using Aurore.Foundation.Core.Privacy;
using Microsoft.AspNetCore.Http;
using Tests.AspNetCore.Support;
using Tests.Shared.Logging;
using Tests.Shared.Privacy;

namespace Tests.AspNetCore.RequestPipeline.Middlewares;

[Trait("Feature", "Request context")]
public sealed class RequestContextMiddlewareTests
{
    public static TheoryData<string> CorrelationIdsInRange => new()
    {
        new string('a', 12),
        "0f8fad5b-d9cb-469f-a165-70867728950e",
        "4bf92f3577b34da6a3ce929d0e0e4736",
        new string('c', 64),
    };

    public static TheoryData<string> IgnoredCorrelationIds => new()
    {
        "X",
        new string('a', 11),
        new string('a', 65),
        "has spaces in it",
        "semi;colon;value",
    };

    public static TheoryData<string> IdempotencyKeysInRange => new()
    {
        new string('a', 12),
        "0f8fad5b-d9cb-469f-a165-70867728950e",
        "order.2026_11-0001",
        new string('k', 128),
    };

    public static TheoryData<string> RefusedIdempotencyKeys => new()
    {
        "X",
        new string('a', 11),
        new string('a', 129),
        "bad key with spaces",
        "bad-key-with-symbol!",
        "chave-com-acentuação",
    };

    [Fact(DisplayName = "The Correlation-Id header wins over X-Request-ID")]
    public async Task CorrelationIdHeaderWins()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Request.Headers["Correlation-Id"] = "from-correlation";
        http.Request.Headers["X-Request-ID"] = "from-request-id";

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal("from-correlation", run.Context.CorrelationId);
    }

    [Fact(DisplayName = "X-Request-ID is used when there is no Correlation-Id")]
    public async Task RequestIdIsUsedAsFallback()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Request.Headers["X-Request-ID"] = "from-request-id";

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal("from-request-id", run.Context.CorrelationId);
    }

    [Fact(DisplayName = "Without a usable header the correlation id is the trace id")]
    public async Task TraceIdIsTheDefault()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.TraceIdentifier = "trace-xyz";

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal("trace-xyz", run.Context.CorrelationId);
        Assert.Equal("trace-xyz", run.Context.TraceId);
    }

    [Theory(DisplayName = "A correlation id is accepted from 12 up to 64 characters")]
    [MemberData(nameof(CorrelationIdsInRange))]
    public async Task CorrelationIdLengthLimitsAreInclusive(string value)
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.TraceIdentifier = "trace-xyz";
        http.Request.Headers["Correlation-Id"] = value;

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal(value, run.Context.CorrelationId);
    }

    [Theory(DisplayName = "A correlation id that is too short, too long or has other characters is ignored")]
    [MemberData(nameof(IgnoredCorrelationIds))]
    public async Task InvalidCorrelationIdsAreIgnored(string value)
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.TraceIdentifier = "trace-xyz";
        http.Request.Headers["Correlation-Id"] = value;

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal("trace-xyz", run.Context.CorrelationId);
    }

    [Fact(DisplayName = "A correlation id that is too short falls back to a valid X-Request-ID before the trace id")]
    public async Task ShortCorrelationIdFallsBackToTheRequestId()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Request.Headers["Correlation-Id"] = "short";
        http.Request.Headers["X-Request-ID"] = "from-request-id";

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal("from-request-id", run.Context.CorrelationId);
    }

    [Theory(DisplayName = "The pattern published in the OpenAPI document accepts exactly the correlation ids the middleware accepts")]
    [MemberData(nameof(CorrelationIdsInRange))]
    [MemberData(nameof(IgnoredCorrelationIds))]
    public async Task PublishedCorrelationPatternAgreesWithTheMiddleware(string value)
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.TraceIdentifier = "trace-xyz";
        http.Request.Headers["Correlation-Id"] = value;

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal(Regex.IsMatch(value, RequestContextMiddleware.CorrelationIdPattern), run.Context.CorrelationId == value);
    }

    [Fact(DisplayName = "The context captures the client address, user agent and host")]
    public async Task ContextCapturesRequestFacts()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        http.Request.Headers.UserAgent = "agent/1.0";
        http.Request.Host = new HostString("example.com");

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal("203.0.113.7", run.Context.ClientAddress);
        Assert.Equal("agent/1.0", run.Context.UserAgent);
        Assert.Equal("example.com", run.Context.Host);
    }

    [Fact(DisplayName = "An oversized user agent is cut to 256 characters")]
    public async Task LongUserAgentIsTruncated()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Request.Headers.UserAgent = new string('a', 1000);

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal(256, run.Context.UserAgent!.Length);
    }

    [Fact(DisplayName = "A valid Idempotency-Key is stored in the context")]
    public async Task ValidIdempotencyKeyIsStored()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Request.Headers["Idempotency-Key"] = "order-2026-123";

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal("order-2026-123", run.Context.IdempotencyKey);
    }

    [Theory(DisplayName = "An Idempotency-Key is accepted from 12 up to 128 characters")]
    [MemberData(nameof(IdempotencyKeysInRange))]
    public async Task IdempotencyKeyLengthLimitsAreInclusive(string key)
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Request.Headers["Idempotency-Key"] = key;

        // Act
        var run = await Run(http);

        // Assert
        Assert.True(run.NextCalled);
        Assert.Equal(key, run.Context.IdempotencyKey);
    }

    [Theory(DisplayName = "An Idempotency-Key that is too short, too long or has other characters is refused with a 400 before the request goes on")]
    [MemberData(nameof(RefusedIdempotencyKeys))]
    public async Task InvalidIdempotencyKeyIsRefused(string key)
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Request.Headers["Idempotency-Key"] = key;

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, http.Response.StatusCode);
        Assert.False(run.NextCalled);
    }

    [Theory(DisplayName = "The pattern published in the OpenAPI document accepts exactly the keys the middleware accepts")]
    [MemberData(nameof(IdempotencyKeysInRange))]
    [MemberData(nameof(RefusedIdempotencyKeys))]
    public async Task PublishedPatternAgreesWithTheMiddleware(string key)
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Request.Headers["Idempotency-Key"] = key;

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal(run.NextCalled, Regex.IsMatch(key, RequestContextMiddleware.IdempotencyKeyPattern));
    }

    [Fact(DisplayName = "With privacy off the log scope carries the real client address and user agent")]
    public async Task ScopeIsReadableWithPrivacyOff()
    {
        // Arrange
        var http = HttpContextFactory.Create();
        http.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        http.Request.Headers.UserAgent = "agent/1.0";

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal("203.0.113.7", run.Scope["client.address"]);
        Assert.Equal("agent/1.0", run.Scope["user_agent.original"]);
    }

    [Fact(DisplayName = "With privacy on the log scope carries encrypted values while the context keeps the real ones")]
    public async Task ScopeIsEncryptedWithPrivacyOn()
    {
        // Arrange
        var privacy = PrivacyTestData.Protector();
        var http = HttpContextFactory.Create();
        http.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        http.Request.Headers.UserAgent = "agent/1.0";
        http.Request.Headers["Correlation-Id"] = "correlation-1";

        // Act
        var run = await Run(http, privacy);

        // Assert
        Assert.NotEqual("203.0.113.7", run.Scope["client.address"]);
        Assert.Equal("203.0.113.7", privacy.Unprotect((string)run.Scope["client.address"]!));
        Assert.Equal("agent/1.0", privacy.Unprotect((string)run.Scope["user_agent.original"]!));
        Assert.Equal("correlation-1", run.Scope["aur.correlation.id"]);
        Assert.Equal("203.0.113.7", run.Context.ClientAddress);
    }

    private static async Task<RunResult> Run(HttpContext http, IPrivacyProtector? privacy = null)
    {
        var logger = new CapturingLogger<RequestContextMiddleware>();
        var nextCalled = false;
        var middleware = new RequestContextMiddleware(
            _ =>
            {
                nextCalled = true;

                return Task.CompletedTask;
            },
            privacy ?? PrivacyTestData.DisabledProtector(),
            logger);
        var context = new RequestContext();

        await middleware.InvokeAsync(http, context);

        return new RunResult(context, logger.Scope, nextCalled);
    }

    private sealed record RunResult(RequestContext Context, Dictionary<string, object?> Scope, bool NextCalled);
}
