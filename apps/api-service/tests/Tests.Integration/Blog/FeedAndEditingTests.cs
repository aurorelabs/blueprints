using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Tests.Integration.Support;

namespace Tests.Integration.Blog;

[Collection(BlogCollection.Name)]
[Trait("Feature", "Posts editing and feed API")]
public sealed class FeedAndEditingTests(BlogFixture blog)
{
    [Fact(DisplayName = "An editor patches a post, a reader cannot, only the fields sent change and the slug stays")]
    public async Task OnlyEditorsUpdate()
    {
        // Arrange
        var (id, slug) = await blog.NewPostAsync(publish: true);
        var body = new { title = "A better title" };

        // Act
        using var asReader = await blog.SendAsync("alice", HttpMethod.Patch, $"/v1/posts/{id}", body);
        using var asEditor = await blog.SendAsync("bob", HttpMethod.Patch, $"/v1/posts/{id}", body);
        using var missing = await blog.SendAsync("bob", HttpMethod.Patch, $"/v1/posts/{Guid.NewGuid()}", body);
        using var invalid = await blog.SendAsync("bob", HttpMethod.Patch, $"/v1/posts/{id}", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, asReader.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asEditor.StatusCode);
        using var json = await BlogFixture.JsonAsync(asEditor);
        Assert.Equal("A better title", json.RootElement.GetProperty("title").GetString());
        Assert.Equal(slug, json.RootElement.GetProperty("slug").GetString());
        Assert.Equal("The text.", json.RootElement.GetProperty("body").GetString());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact(DisplayName = "Only an admin deletes a comment, and a deleted comment leaves the list")]
    public async Task OnlyAdminsDeleteComments()
    {
        // Arrange
        var (postId, _) = await blog.NewPostAsync(publish: true);
        using var created = await blog.SendAsync("alice", HttpMethod.Post, $"/v1/posts/{postId}/comments", new { body = "Delete me." });
        using var createdJson = await BlogFixture.JsonAsync(created);
        var commentId = createdJson.RootElement.GetProperty("id").GetGuid();

        // Act
        using var byReader = await blog.SendAsync("alice", HttpMethod.Delete, $"/v1/posts/{postId}/comments/{commentId}");
        using var byAdmin = await blog.SendAsync("bob", HttpMethod.Delete, $"/v1/posts/{postId}/comments/{commentId}");
        using var again = await blog.SendAsync("bob", HttpMethod.Delete, $"/v1/posts/{postId}/comments/{commentId}");
        using var list = await blog.SendAsync("alice", HttpMethod.Get, $"/v1/posts/{postId}/comments");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, byReader.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, byAdmin.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        using var listJson = await BlogFixture.JsonAsync(list);
        Assert.Empty(listJson.RootElement.GetProperty("items").EnumerateArray());
    }
    [Fact(DisplayName = "The list can be filtered by the handle of the author")]
    public async Task ListFiltersByAuthor()
    {
        // Arrange
        var token = $"tok{Guid.NewGuid().ToString("N")[..10]}";
        await CreatePublishedAsync($"{token} one");
        await CreatePublishedAsync($"{token} two");

        // Act
        using var byBob = await blog.SendAsync("carol", HttpMethod.Get, $"/v1/posts?title={token}&author={blog.HandleOf("bob")}&orderBy=title");
        using var byNobody = await blog.SendAsync("carol", HttpMethod.Get, $"/v1/posts?title={token}&author=nobody");

        // Assert
        using var bobJson = await BlogFixture.JsonAsync(byBob);
        Assert.Equal([$"{token} one", $"{token} two"], Titles(bobJson));
        using var nobodyJson = await BlogFixture.JsonAsync(byNobody);
        Assert.Empty(Titles(nobodyJson));
    }


    [Fact(DisplayName = "The feed is an infinite scroll of the published posts, newest first, without drafts")]
    public async Task FeedIsAnInfiniteScrollOfPublishedPosts()
    {
        // Arrange
        var token = $"tok{Guid.NewGuid().ToString("N")[..10]}";

        for (var i = 0; i < 5; i++)
            await CreatePublishedAsync($"{token} {i}");

        using var draft = await blog.SendAsync("bob", HttpMethod.Post, "/v1/posts", new { slug = BlogFixture.NewSlug(), title = $"{token} draft", body = "Text" });
        draft.EnsureSuccessStatusCode();

        // Act
        var seen = new List<string>();
        string? cursor = null;
        var pages = 0;

        do
        {
            var path = $"/v1/feed?title={token}&pageSize=2" + (cursor is null ? string.Empty : $"&cursor={Uri.EscapeDataString(cursor)}");
            using var response = await blog.SendAsync("carol", HttpMethod.Get, path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = await BlogFixture.JsonAsync(response);
            seen.AddRange(Titles(json));
            cursor = json.RootElement.TryGetProperty("nextCursor", out var next) && next.ValueKind is JsonValueKind.String ? next.GetString() : null;
            pages++;
        }
        while (cursor is not null);

        // Assert
        Assert.Equal(Enumerable.Range(0, 5).Reverse().Select(i => $"{token} {i}"), seen);
        Assert.Equal(3, pages);
    }

    [Fact(DisplayName = "The feed refuses an ordering it cannot page by, and a cursor used with other filters")]
    public async Task FeedRefusesWhatItCannotPage()
    {
        // Arrange
        var token = $"tok{Guid.NewGuid().ToString("N")[..10]}";
        await CreatePublishedAsync($"{token} one");
        await CreatePublishedAsync($"{token} two");
        using var first = await blog.SendAsync("carol", HttpMethod.Get, $"/v1/feed?title={token}&pageSize=1");
        using var firstJson = await BlogFixture.JsonAsync(first);
        var cursor = firstJson.RootElement.GetProperty("nextCursor").GetString()!;

        // Act
        using var byPublishedAt = await blog.SendAsync("carol", HttpMethod.Get, "/v1/feed?orderBy=publishedAt");
        using var otherFilter = await blog.SendAsync("carol", HttpMethod.Get, $"/v1/feed?title=other&cursor={Uri.EscapeDataString(cursor)}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, byPublishedAt.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, otherFilter.StatusCode);
    }

    private async Task CreatePublishedAsync(string title)
    {
        using var created = await blog.SendAsync("bob", HttpMethod.Post, "/v1/posts", new { slug = BlogFixture.NewSlug(), title, body = "Text" });
        created.EnsureSuccessStatusCode();
        using var json = await BlogFixture.JsonAsync(created);
        using var published = await blog.SendAsync("bob", HttpMethod.Post, $"/v1/posts/{json.RootElement.GetProperty("id").GetGuid()}/publish");
        published.EnsureSuccessStatusCode();
    }

    private static IEnumerable<string> Titles(JsonDocument json)
    {
        return json.RootElement.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("title").GetString()!);
    }
}
