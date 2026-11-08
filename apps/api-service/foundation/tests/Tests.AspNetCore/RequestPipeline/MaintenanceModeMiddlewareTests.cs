using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.AspNetCore.RequestPipeline;

[Trait("Feature", "Maintenance mode")]
public sealed class MaintenanceModeMiddlewareTests
{
    [Fact(DisplayName = "Requests get a 503 with the configured Retry-After")]
    public async Task RequestsGetA503WithRetryAfter()
    {
        var context = CreateContext("/api/orders");
        var middleware = new MaintenanceModeMiddleware(_ => Task.CompletedTask, new MaintenanceModeOptions { Enabled = true, RetryAfterSeconds = 120 });

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        Assert.Equal("120", context.Response.Headers.RetryAfter.ToString());
    }

    [Fact(DisplayName = "Health check requests still pass through during maintenance")]
    public async Task HealthChecksPassThrough()
    {
        var context = CreateContext("/health/ready");
        var reachedNext = false;
        var middleware = new MaintenanceModeMiddleware(_ =>
        {
            reachedNext = true;

            return Task.CompletedTask;
        }, new MaintenanceModeOptions { Enabled = true });

        await middleware.InvokeAsync(context);

        Assert.True(reachedNext);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        return context;
    }
}
