using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Tests.Integration.Support;

namespace Tests.Integration.Blog;

[Collection(BlogCollection.Name)]
[Trait("Feature", "Comments API")]
public sealed class CommentsEndpointsTests(BlogFixture blog)
{
    [Fact(DisplayName = "Someone with no author profile cannot comment: 409 with the code that says to create one")]
    public async Task CommentNeedsAProfile()
    {
        // Arrange
        var (id, _) = await blog.NewPostAsync(publish: true);

        // Act
        using var response = await blog.SendAsync("carol", HttpMethod.Post, $"/v1/posts/{id}/comments", new { body = "Nice." });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = await BlogFixture.JsonAsync(response);
        Assert.Equal("author.profile_required", json.RootElement.GetProperty("code").GetString());
    }

    [Fact(DisplayName = "A reader with a profile comments on a published post, and cannot on a draft")]
    public async Task CommentsOnPublishedPostsOnly()
    {
        // Arrange
        var (draft, _) = await blog.NewPostAsync(publish: false);
        var (published, _) = await blog.NewPostAsync(publish: true);

        // Act
        using var onDraft = await blog.SendAsync("alice", HttpMethod.Post, $"/v1/posts/{draft}/comments", new { body = "Early." });
        using var onPublished = await blog.SendAsync("alice", HttpMethod.Post, $"/v1/posts/{published}/comments", new { body = "Nice." });

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, onDraft.StatusCode);
        Assert.Equal(HttpStatusCode.Created, onPublished.StatusCode);
        using var json = await BlogFixture.JsonAsync(onPublished);
        Assert.Equal(blog.HandleOf("alice"), json.RootElement.GetProperty("authorHandle").GetString());
    }

    [Fact(DisplayName = "An empty comment is a 400, and a post that does not exist is a 404")]
    public async Task InvalidAndMissing()
    {
        // Arrange
        var (id, _) = await blog.NewPostAsync(publish: true);

        // Act
        using var empty = await blog.SendAsync("alice", HttpMethod.Post, $"/v1/posts/{id}/comments", new { body = "" });
        using var missing = await blog.SendAsync("alice", HttpMethod.Post, $"/v1/posts/{Guid.NewGuid()}/comments", new { body = "Hello?" });
        using var listMissing = await blog.SendAsync("alice", HttpMethod.Get, $"/v1/posts/{Guid.NewGuid()}/comments");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, listMissing.StatusCode);
    }

    [Fact(DisplayName = "Repeating a comment with the same Idempotency-Key leaves one comment")]
    public async Task RepeatedCommentIsIdempotent()
    {
        // Arrange
        var (id, _) = await blog.NewPostAsync(publish: true);
        var key = $"comment-{Guid.NewGuid():N}";

        // Act
        using var first = await blog.SendAsync("alice", HttpMethod.Post, $"/v1/posts/{id}/comments", new { body = "Once." }, key);
        using var second = await blog.SendAsync("alice", HttpMethod.Post, $"/v1/posts/{id}/comments", new { body = "Once." }, key);
        using var list = await blog.SendAsync("alice", HttpMethod.Get, $"/v1/posts/{id}/comments");

        // Assert
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        using var json = await BlogFixture.JsonAsync(list);
        Assert.Single(json.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact(DisplayName = "Following nextCursor lists every comment once, oldest first, as an infinite scroll")]
    public async Task CursorListsEveryComment()
    {
        // Arrange
        var (id, _) = await blog.NewPostAsync(publish: true);
        var written = new List<string>();

        for (var i = 0; i < 5; i++)
        {
            using var created = await blog.SendAsync(i % 2 == 0 ? "alice" : "bob", HttpMethod.Post, $"/v1/posts/{id}/comments", new { body = $"Comment {i}" });
            created.EnsureSuccessStatusCode();
            written.Add($"Comment {i}");
        }

        // Act
        var seen = new List<string>();
        string? cursor = null;
        var pages = 0;

        do
        {
            var path = $"/v1/posts/{id}/comments?pageSize=2" + (cursor is null ? string.Empty : $"&cursor={Uri.EscapeDataString(cursor)}");
            using var response = await blog.SendAsync("carol", HttpMethod.Get, path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = await BlogFixture.JsonAsync(response);
            seen.AddRange(json.RootElement.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("body").GetString()!));
            cursor = json.RootElement.TryGetProperty("nextCursor", out var next) && next.ValueKind is System.Text.Json.JsonValueKind.String ? next.GetString() : null;
            pages++;
        }
        while (cursor is not null);

        // Assert
        Assert.Equal(written, seen);
        Assert.Equal(3, pages);
    }
}
