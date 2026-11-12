using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Tests.Integration.Support;

namespace Tests.Integration.Blog;

[Collection(BlogCollection.Name)]
[Trait("Feature", "Posts API")]
public sealed class PostsEndpointsTests(BlogFixture blog)
{
    [Fact(DisplayName = "A reader cannot write a post, an editor can, and the answer points at the new post")]
    public async Task OnlyEditorsWrite()
    {
        // Arrange
        var body = new { slug = BlogFixture.NewSlug(), title = "A title", body = "The text." };

        // Act
        using var asReader = await blog.SendAsync("alice", HttpMethod.Post, "/v1/posts", body);
        using var asEditor = await blog.SendAsync("bob", HttpMethod.Post, "/v1/posts", body);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, asReader.StatusCode);
        Assert.Equal(HttpStatusCode.Created, asEditor.StatusCode);
        Assert.Equal($"/v1/posts/{body.slug}", asEditor.Headers.Location!.ToString());
        using var json = await BlogFixture.JsonAsync(asEditor);
        Assert.Equal("Draft", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(blog.HandleOf("bob"), json.RootElement.GetProperty("authorHandle").GetString());
    }

    [Fact(DisplayName = "A body that breaks the rules is a 400 with every field named")]
    public async Task InvalidBodyIsRefusedWithTheFields()
    {
        // Arrange
        // Act
        using var response = await blog.SendAsync("bob", HttpMethod.Post, "/v1/posts", new { slug = "Not a slug", title = "", body = "text" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = await BlogFixture.JsonAsync(response);
        var errors = json.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("slug", out _));
        Assert.True(errors.TryGetProperty("title", out _));
    }

    [Fact(DisplayName = "A slug already in use is a 409 with the code of the error, and a deleted post gives its slug back")]
    public async Task SlugConflictAndRelease()
    {
        // Arrange
        var (id, slug) = await blog.NewPostAsync(publish: false);
        var again = new { slug, title = "Same slug", body = "Text" };

        // Act
        using var conflict = await blog.SendAsync("bob", HttpMethod.Post, "/v1/posts", again);
        using var deleted = await blog.SendAsync("bob", HttpMethod.Delete, $"/v1/posts/{id}");
        using var reused = await blog.SendAsync("bob", HttpMethod.Post, "/v1/posts", again);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var json = await BlogFixture.JsonAsync(conflict);
        Assert.Equal("post.slug_taken", json.RootElement.GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.Created, reused.StatusCode);
    }

    [Fact(DisplayName = "Repeating a creation with the same Idempotency-Key gives the first answer back and creates one post")]
    public async Task RepeatedCreationIsIdempotent()
    {
        // Arrange
        var body = new { slug = BlogFixture.NewSlug(), title = "Once", body = "Text" };
        var key = $"create-{Guid.NewGuid():N}";

        // Act
        using var first = await blog.SendAsync("bob", HttpMethod.Post, "/v1/posts", body, key);
        using var second = await blog.SendAsync("bob", HttpMethod.Post, "/v1/posts", body, key);

        // Assert
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.True(second.Headers.Contains("Idempotency-Replayed"));
        using var firstJson = await BlogFixture.JsonAsync(first);
        using var secondJson = await BlogFixture.JsonAsync(second);
        Assert.Equal(firstJson.RootElement.GetProperty("id").GetGuid(), secondJson.RootElement.GetProperty("id").GetGuid());
    }

    [Fact(DisplayName = "A draft is visible to its author only until it is published")]
    public async Task DraftIsPrivateUntilPublished()
    {
        // Arrange
        var (id, slug) = await blog.NewPostAsync(publish: false);

        // Act
        using var otherBefore = await blog.SendAsync("alice", HttpMethod.Get, $"/v1/posts/{slug}");
        using var authorBefore = await blog.SendAsync("bob", HttpMethod.Get, $"/v1/posts/{slug}");
        using var published = await blog.SendAsync("bob", HttpMethod.Post, $"/v1/posts/{id}/publish");
        using var otherAfter = await blog.SendAsync("alice", HttpMethod.Get, $"/v1/posts/{slug}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, otherBefore.StatusCode);
        Assert.Equal(HttpStatusCode.OK, authorBefore.StatusCode);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherAfter.StatusCode);
    }

    [Fact(DisplayName = "A post cannot be published twice, and only the author or an admin publishes")]
    public async Task PublishRules()
    {
        // Arrange
        var (id, _) = await blog.NewPostAsync(publish: false);

        // Act
        using var byReader = await blog.SendAsync("alice", HttpMethod.Post, $"/v1/posts/{id}/publish");
        using var first = await blog.SendAsync("bob", HttpMethod.Post, $"/v1/posts/{id}/publish");
        using var second = await blog.SendAsync("bob", HttpMethod.Post, $"/v1/posts/{id}/publish");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, byReader.StatusCode);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        using var json = await BlogFixture.JsonAsync(second);
        Assert.Equal("post.already_published", json.RootElement.GetProperty("code").GetString());
    }

    [Fact(DisplayName = "Only an admin deletes, and a deleted post is gone for readers")]
    public async Task OnlyAdminsDelete()
    {
        // Arrange
        var (id, slug) = await blog.NewPostAsync(publish: true);

        // Act
        using var byReader = await blog.SendAsync("alice", HttpMethod.Delete, $"/v1/posts/{id}");
        using var byAdmin = await blog.SendAsync("bob", HttpMethod.Delete, $"/v1/posts/{id}");
        using var read = await blog.SendAsync("alice", HttpMethod.Get, $"/v1/posts/{slug}");
        using var again = await blog.SendAsync("bob", HttpMethod.Delete, $"/v1/posts/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, byReader.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact(DisplayName = "The list is a page with its totals, filtered by title, and a bad ordering is a 400")]
    public async Task ListIsPagedFilteredAndValidated()
    {
        // Arrange
        var token = $"tok{Guid.NewGuid().ToString("N")[..10]}";

        for (var i = 0; i < 3; i++)
        {
            using var created = await blog.SendAsync("bob", HttpMethod.Post, "/v1/posts", new { slug = BlogFixture.NewSlug(), title = $"{token} number {i}", body = "Text" });
            using var json = await BlogFixture.JsonAsync(created);
            using var published = await blog.SendAsync("bob", HttpMethod.Post, $"/v1/posts/{json.RootElement.GetProperty("id").GetGuid()}/publish");
        }

        // Act
        using var page = await blog.SendAsync("alice", HttpMethod.Get, $"/v1/posts?title={token}&pageSize=2&orderBy=title");
        using var badOrder = await blog.SendAsync("alice", HttpMethod.Get, "/v1/posts?orderBy=body");

        // Assert
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        using var body = await BlogFixture.JsonAsync(page);
        Assert.Equal(3, body.RootElement.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, body.RootElement.GetProperty("totalPages").GetInt32());
        Assert.Equal(1, body.RootElement.GetProperty("pageNumber").GetInt32());
        Assert.Equal([$"{token} number 0", $"{token} number 1"], body.RootElement.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("title").GetString()));
        Assert.Equal(HttpStatusCode.BadRequest, badOrder.StatusCode);
    }

    [Fact(DisplayName = "The API key client can read posts, through the read scope")]
    public async Task ApiKeyClientReads()
    {
        // Arrange
        var (_, slug) = await blog.NewPostAsync(publish: true);

        // Act
        using var list = await blog.SendWithApiKeyAsync(HttpMethod.Get, "/v1/posts");
        using var one = await blog.SendWithApiKeyAsync(HttpMethod.Get, $"/v1/posts/{slug}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, one.StatusCode);
    }
}
