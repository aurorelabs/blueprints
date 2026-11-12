using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Features.Posts;
using Aurore.Foundation.Core.Pagination.Models;
using Tests.Features.Support;

namespace Tests.Features.Posts;

[Trait("Feature", "Post feed")]
public sealed class StreamPostsTests(FeaturesFixture features) : IClassFixture<FeaturesFixture>
{
    [Fact(DisplayName = "Following the cursor lists every published post once, newest first, and never a draft")]
    public async Task CursorListsThePublishedPostsNewestFirst()
    {
        // Arrange
        var token = Token();
        var writer = await features.NewAuthorAsync();
        var published = new List<Guid>();

        for (var i = 0; i < 5; i++)
        {
            features.Time.Advance(TimeSpan.FromSeconds(1));
            published.Add((await features.NewPostAsync(writer, publish: true, title: $"{token} {i}")).Id);
        }

        await features.NewPostAsync(writer, title: $"{token} a draft");

        // Act
        var seen = await Walk(new StreamPostsRequest { Title = token, PageSize = 2 });

        // Assert
        published.Reverse();
        Assert.Equal(published, seen);
    }

    [Fact(DisplayName = "The feed can be filtered by author and ordered by title or author")]
    public async Task FeedFiltersByAuthorAndOrders()
    {
        // Arrange
        var token = Token();
        var first = await features.NewAuthorAsync("first");
        var second = await features.NewAuthorAsync("second");
        var byFirst = await features.NewPostAsync(first, publish: true, title: $"{token} Zulu");
        var bySecond = await features.NewPostAsync(second, publish: true, title: $"{token} Alpha");

        // Act
        var onlyFirst = await Walk(new StreamPostsRequest { Title = token, Author = first.Profile.Handle });
        var byTitle = await Walk(new StreamPostsRequest { Title = token, OrderBy = ["title"] });
        var byAuthor = await Walk(new StreamPostsRequest { Title = token, OrderBy = ["author"] });

        // Assert
        Assert.Equal([byFirst.Id], onlyFirst);
        Assert.Equal([bySecond.Id, byFirst.Id], byTitle);
        Assert.Equal([byFirst.Id, bySecond.Id], byAuthor);
    }

    [Fact(DisplayName = "The page reports the cursor of the next one only when there is one")]
    public async Task LastPageHasNoCursor()
    {
        // Arrange
        var token = Token();
        var writer = await features.NewAuthorAsync();
        await features.NewPostAsync(writer, publish: true, title: $"{token} one");
        await features.NewPostAsync(writer, publish: true, title: $"{token} two");

        // Act
        var first = await features.RunAsync<StreamPosts, StreamPostsRequest, CursorPage<PostSummary>>(new StreamPostsRequest { Title = token, PageSize = 1 });
        var all = await features.RunAsync<StreamPosts, StreamPostsRequest, CursorPage<PostSummary>>(new StreamPostsRequest { Title = token, PageSize = 2 });

        // Assert
        Assert.NotNull(first.Value.NextCursor);
        Assert.Null(all.Value.NextCursor);
    }

    [Fact(DisplayName = "A feed cannot be ordered by the publication date, which can be missing, and says so")]
    public async Task OrderingByPublicationDateIsRefused()
    {
        // Arrange
        var request = new StreamPostsRequest { OrderBy = ["publishedAt"] };

        // Act
        var result = await features.RunAsync<StreamPosts, StreamPostsRequest, CursorPage<PostSummary>>(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("orderBy", result.Error!.Details.Keys);
    }

    private async Task<List<Guid>> Walk(StreamPostsRequest first)
    {
        var seen = new List<Guid>();
        var request = first;

        while (true)
        {
            var result = await features.RunAsync<StreamPosts, StreamPostsRequest, CursorPage<PostSummary>>(request);

            Assert.True(result.IsSuccess, result.Error?.Message);
            seen.AddRange(result.Value.Items.Select(x => x.Id));

            if (result.Value.NextCursor is null)
                return seen;

            request = new StreamPostsRequest { Title = first.Title, Author = first.Author, OrderBy = first.OrderBy, PageSize = first.PageSize, Cursor = result.Value.NextCursor };
        }
    }

    private static string Token()
    {
        return $"tok{Guid.NewGuid().ToString("N")[..10]}";
    }
}
