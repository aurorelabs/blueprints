using Aurore.Foundation.AspNetCore.RequestPipeline.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.AspNetCore.Support;

namespace Tests.AspNetCore.RequestPipeline.Handlers;

[Trait("Feature", "Exception handler")]
public sealed class UnexpectedExceptionHandlerTests
{
    private readonly UnexpectedExceptionHandler _handler = new(NullLogger<UnexpectedExceptionHandler>.Instance);

    [Fact(DisplayName = "An unexpected exception becomes a 500 without exposing its message")]
    public async Task UnexpectedExceptionBecomesA500()
    {
        // Arrange
        var http = CreateContext();
        var exception = new InvalidOperationException("secret connection string");

        // Act
        var handled = await _handler.TryHandleAsync(http, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, http.Response.StatusCode);
        Assert.DoesNotContain("secret", await HttpContextFactory.ReadBodyAsync(http));
    }

    [Fact(DisplayName = "A malformed request keeps the status the framework chose")]
    public async Task MalformedRequestKeepsItsStatus()
    {
        // Arrange
        var http = CreateContext();
        var exception = new BadHttpRequestException("Request body too large.", StatusCodes.Status413PayloadTooLarge);

        // Act
        await _handler.TryHandleAsync(http, exception, CancellationToken.None);

        // Assert
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, http.Response.StatusCode);
    }

    [Fact(DisplayName = "A caller that closed the request gets a 499 and no problem body")]
    public async Task ClosedRequestGetsA499()
    {
        // Arrange
        var http = CreateContext();
        http.RequestAborted = new CancellationToken(canceled: true);

        // Act
        var handled = await _handler.TryHandleAsync(http, new OperationCanceledException(), CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(UnexpectedExceptionHandler.ClientClosedRequestStatusCode, http.Response.StatusCode);
        Assert.Equal(string.Empty, await HttpContextFactory.ReadBodyAsync(http));
    }

    private static DefaultHttpContext CreateContext()
    {
        return HttpContextFactory.Create(configure: services => services.AddProblemDetails());
    }
}
