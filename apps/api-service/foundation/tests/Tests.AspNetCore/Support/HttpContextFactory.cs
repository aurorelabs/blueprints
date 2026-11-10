using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.AspNetCore.Support;

/// <summary>
/// Builds the HTTP contexts the tests run components against, so no test sets one up by hand.
/// </summary>
internal static class HttpContextFactory
{
    /// <summary>
    /// A context with logging available and a writable response body. An authenticated user is added when a name is given.
    /// </summary>
    public static DefaultHttpContext Create(string? user = null, string method = "GET", string path = "/", Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection().AddLogging();
        configure?.Invoke(services);

        return Create(services.BuildServiceProvider(), user, method, path);
    }

    /// <summary>
    /// A context that resolves its services from the given provider, for tests that need several requests to share state.
    /// </summary>
    public static DefaultHttpContext Create(IServiceProvider services, string? user = null, string method = "GET", string path = "/")
    {
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = method;
        http.Request.Path = path;
        http.Response.Body = new MemoryStream();

        if (user is not null)
        {
            http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user)], "test"));
        }

        return http;
    }

    /// <summary>
    /// Everything written to the response body so far.
    /// </summary>
    public static async Task<string> ReadBodyAsync(HttpContext http)
    {
        http.Response.Body.Position = 0;

        return await new StreamReader(http.Response.Body).ReadToEndAsync(TestContext.Current.CancellationToken);
    }
}
