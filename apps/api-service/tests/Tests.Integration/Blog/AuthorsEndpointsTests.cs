using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Tests.Integration.Support;

namespace Tests.Integration.Blog;

[Collection(BlogCollection.Name)]
[Trait("Feature", "Authors API")]
public sealed class AuthorsEndpointsTests(BlogFixture blog)
{
    [Fact(DisplayName = "Without credentials the profile endpoint answers 401")]
    public async Task WithoutCredentialsTheAnswerIs401()
    {
        // Arrange
        // Act
        using var response = await blog.SendAsync(user: null, HttpMethod.Put, "/v1/authors/me", new { handle = "nobody", displayName = "Nobody" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "A signed-in user creates the profile, and sending it again changes nothing")]
    public async Task ProfileIsCreatedAndRepeatable()
    {
        // Arrange
        var body = new { handle = $"dave-{System.Guid.NewGuid().ToString("N")[..8]}", displayName = "Dave" };

        // Act
        using var first = await blog.SendAsync("dave", HttpMethod.Put, "/v1/authors/me", body);
        using var second = await blog.SendAsync("dave", HttpMethod.Put, "/v1/authors/me", body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        using var firstJson = await BlogFixture.JsonAsync(first);
        using var secondJson = await BlogFixture.JsonAsync(second);
        Assert.Equal(body.handle, firstJson.RootElement.GetProperty("handle").GetString());
        Assert.Equal(firstJson.RootElement.GetProperty("id").GetGuid(), secondJson.RootElement.GetProperty("id").GetGuid());
    }

    [Fact(DisplayName = "A handle that is not lower case letters, digits and single hyphens is a 400 naming the field")]
    public async Task InvalidHandleIsRefusedWithTheField()
    {
        // Arrange
        // Act
        using var response = await blog.SendAsync("dave", HttpMethod.Put, "/v1/authors/me", new { handle = "Not A Handle!", displayName = "Dave" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await BlogFixture.JsonAsync(response);
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty("handle", out _));
    }

    [Fact(DisplayName = "An author that has a handle cannot take another one")]
    public async Task HandleCannotChange()
    {
        // Arrange
        // alice has her profile from the start of the collection
        // Act
        using var response = await blog.SendAsync("alice", HttpMethod.Put, "/v1/authors/me", new { handle = "a-new-handle", displayName = "Alice" });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = await BlogFixture.JsonAsync(response);
        Assert.Equal("author.handle_cannot_change", json.RootElement.GetProperty("code").GetString());
    }

    [Fact(DisplayName = "The API key client has no write scope, so it cannot create a profile")]
    public async Task ApiKeyClientCannotWrite()
    {
        // Arrange
        // Act
        using var response = await blog.SendWithApiKeyAsync(HttpMethod.Put, "/v1/authors/me", new { handle = "key-client", displayName = "Key" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
