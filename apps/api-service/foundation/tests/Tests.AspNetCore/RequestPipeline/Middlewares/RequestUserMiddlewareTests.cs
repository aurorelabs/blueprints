using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.Models;
using Aurore.Foundation.Core.Privacy;
using Microsoft.AspNetCore.Http;
using Tests.AspNetCore.Support;
using Tests.Shared.Logging;
using Tests.Shared.Privacy;

namespace Tests.AspNetCore.RequestPipeline.Middlewares;

[Trait("Feature", "Request context")]
public sealed class RequestUserMiddlewareTests
{
    [Fact(DisplayName = "An authenticated user's id is stored in the context and the log scope")]
    public async Task UserIdIsStored()
    {
        // Arrange
        var http = HttpContextFactory.Create(user: "user-1");

        // Act
        var run = await Run(http);

        // Assert
        Assert.Equal("user-1", run.Context.UserId);
        Assert.Equal("user-1", run.Scope["enduser.id"]);
        Assert.True(run.NextCalled);
    }

    [Fact(DisplayName = "An anonymous request leaves the user id empty and carries on")]
    public async Task AnonymousRequestHasNoUserId()
    {
        // Arrange
        var http = HttpContextFactory.Create();

        // Act
        var run = await Run(http);

        // Assert
        Assert.Null(run.Context.UserId);
        Assert.Empty(run.Scope);
        Assert.True(run.NextCalled);
    }

    [Fact(DisplayName = "With privacy on the user id in the log scope is encrypted while the context keeps the real one")]
    public async Task UserIdIsEncryptedInTheScope()
    {
        // Arrange
        var privacy = PrivacyTestData.Protector();
        var http = HttpContextFactory.Create(user: "user-1");

        // Act
        var run = await Run(http, privacy);

        // Assert
        Assert.Equal("user-1", run.Context.UserId);
        Assert.NotEqual("user-1", run.Scope["enduser.id"]);
        Assert.Equal("user-1", privacy.Unprotect((string)run.Scope["enduser.id"]!));
    }

    private static async Task<RunResult> Run(HttpContext http, IPrivacyProtector? privacy = null)
    {
        var logger = new CapturingLogger<RequestUserMiddleware>();
        var nextCalled = false;
        var middleware = new RequestUserMiddleware(
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
