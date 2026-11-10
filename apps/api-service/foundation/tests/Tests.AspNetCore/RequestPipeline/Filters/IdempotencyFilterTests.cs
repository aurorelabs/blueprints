using Aurore.Foundation.AspNetCore.RequestPipeline.Filters;
using Aurore.Foundation.Core.Models;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Tests.AspNetCore.Support;

namespace Tests.AspNetCore.RequestPipeline.Filters;

[Trait("Feature", "Idempotency")]
public sealed class IdempotencyFilterTests : IAsyncDisposable
{
    private readonly ServiceProvider _provider = CreateProvider();

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
    }

    [Fact(DisplayName = "The same caller repeating a key gets the first result without running the endpoint again")]
    public async Task SameCallerReplaysTheFirstResult()
    {
        // Arrange
        var calls = 0;

        // Act
        await Invoke("alice", "key-1", _ => Ok(++calls));
        var second = await Invoke("alice", "key-1", _ => Ok(++calls));

        // Assert
        Assert.Equal(1, calls);
        Assert.Equal(StatusCodes.Status200OK, StatusOf(second));
    }

    [Fact(DisplayName = "A different caller using the same key does not receive someone else's result")]
    public async Task DifferentCallersDoNotShareResults()
    {
        // Arrange
        var calls = 0;

        // Act
        await Invoke("alice", "key-1", _ => Ok(++calls));
        await Invoke("bob", "key-1", _ => Ok(++calls));

        // Assert
        Assert.Equal(2, calls);
    }

    [Fact(DisplayName = "The same key on a different path is a different request")]
    public async Task DifferentPathsDoNotShareResults()
    {
        // Arrange
        var calls = 0;

        // Act
        await Invoke("alice", "key-1", _ => Ok(++calls), path: "/orders");
        await Invoke("alice", "key-1", _ => Ok(++calls), path: "/payments");

        // Assert
        Assert.Equal(2, calls);
    }

    [Fact(DisplayName = "Failed results are not stored, so the request can be retried")]
    public async Task FailuresAreNotCached()
    {
        // Arrange
        var calls = 0;

        // Act
        await Invoke("alice", "key-1", _ =>
        {
            calls++;

            return TypedResults.Conflict();
        });
        await Invoke("alice", "key-1", _ =>
        {
            calls++;

            return TypedResults.Conflict();
        });

        // Assert
        Assert.Equal(2, calls);
    }

    [Fact(DisplayName = "Without a key the endpoint runs every time")]
    public async Task NoKeyRunsEveryTime()
    {
        // Arrange
        var calls = 0;

        // Act
        await Invoke("alice", null, _ => Ok(++calls));
        await Invoke("alice", null, _ => Ok(++calls));

        // Assert
        Assert.Equal(2, calls);
    }

    [Fact(DisplayName = "When the key is required, a request without it gets a 400 and the endpoint does not run")]
    public async Task RequiredKeyIsEnforced()
    {
        // Arrange
        var calls = 0;

        // Act
        var result = await Invoke("alice", null, _ => Ok(++calls), required: true);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<ProblemHttpResult>(result).StatusCode);
        Assert.Equal(0, calls);
    }

    [Fact(DisplayName = "A handler that returns a plain object is stored too")]
    public async Task PlainObjectsAreStored()
    {
        // Arrange
        var calls = 0;

        // Act
        await Invoke("alice", "key-1", _ => new { value = ++calls });
        var second = await Invoke("alice", "key-1", _ => new { value = ++calls });

        // Assert
        Assert.Equal(1, calls);
        Assert.Equal(StatusCodes.Status200OK, StatusOf(second));
    }

    [Fact(DisplayName = "A replay keeps the status, body, Location and headers the endpoint added, and says it is a replay")]
    public async Task ReplayKeepsStatusBodyAndHeaders()
    {
        // Arrange
        var calls = 0;
        object? Endpoint(EndpointFilterInvocationContext context)
        {
            calls++;
            context.HttpContext.Response.Headers["X-Custom"] = "demo";

            return TypedResults.Created("/orders/1", new { id = 1 });
        }

        await Invoke("alice", "key-1", Endpoint);
        var replay = await Invoke("alice", "key-1", Endpoint);
        var response = HttpContextFactory.Create(_provider, "alice", "POST", "/orders");

        // Act
        await ((IResult)replay!).ExecuteAsync(response);

        // Assert
        Assert.Equal(1, calls);
        Assert.Equal(StatusCodes.Status201Created, response.Response.StatusCode);
        Assert.Equal("/orders/1", response.Response.Headers.Location.ToString());
        Assert.Equal("demo", response.Response.Headers["X-Custom"].ToString());
        Assert.Equal("true", response.Response.Headers["Idempotency-Replayed"].ToString());
        Assert.Contains("\"id\":1", await HttpContextFactory.ReadBodyAsync(response));
    }

    [Fact(DisplayName = "The first response is the endpoint's own result, not a replay")]
    public async Task FirstResponseIsTheEndpointsOwnResult()
    {
        // Arrange
        var own = Ok(1);

        // Act
        var first = await Invoke("alice", "key-1", _ => own);

        // Assert
        Assert.Same(own, first);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddHybridCache();

        return services.BuildServiceProvider();
    }

    private static IResult Ok(int value)
    {
        return TypedResults.Ok(new { value });
    }

    private static int? StatusOf(object? result)
    {
        return Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode;
    }

    private async Task<object?> Invoke(
        string user,
        string? idempotencyKey,
        Func<EndpointFilterInvocationContext, object?> endpoint,
        string path = "/orders",
        bool required = false)
    {
        var requestContext = new RequestContext();
        requestContext.Initialize("c", "t", null, idempotencyKey, null, null, null);

        var filter = new IdempotencyFilter(_provider.GetRequiredService<HybridCache>(), new IdempotencyOptions(), requestContext);
        var http = HttpContextFactory.Create(_provider, user, "POST", path);
        http.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new IdempotencyMetadata(required)), "test"));

        return await filter.InvokeAsync(EndpointFilterInvocationContext.Create(http), context => ValueTask.FromResult(endpoint(context)));
    }
}
