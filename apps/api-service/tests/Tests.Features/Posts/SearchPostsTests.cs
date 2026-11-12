using System;
using System.Linq;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Domain;
using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Blueprints.ApiService.Features.Posts;
using Aurore.Foundation.Core.Pagination.Models;
using Tests.Features.Support;

namespace Tests.Features.Posts;

[Trait("Feature", "Post list")]
public sealed class SearchPostsTests(FeaturesFixture features) : IClassFixture<FeaturesFixture>
{
    [Fact(DisplayName = "The list shows published posts to everyone, and the drafts of the caller only")]
    public async Task ListShowsPublishedPostsAndOwnDrafts()
    {
        // Arrange
        var token = Token();
        var me = await features.NewAuthorAsync();
        var someone = await features.NewAuthorAsync();
        var published = await features.NewPostAsync(someone, publish: true, title: $"{token} published");
        var myDraft = await features.NewPostAsync(me, title: $"{token} my draft");
        var theirDraft = await features.NewPostAsync(someone, title: $"{token} their draft");

        // Act
        var page = await Search(token, me.Caller);

        // Assert
        Assert.Equivalent(new[] { published.Id, myDraft.Id }, page.Items.Select(x => x.Id));
        Assert.DoesNotContain(page.Items, x => x.Id == theirDraft.Id);
    }

    [Fact(DisplayName = "An admin sees every draft")]
    public async Task AdminSeesEveryDraft()
    {
        // Arrange
        var token = Token();
        var writer = await features.NewAuthorAsync();
        await features.NewPostAsync(writer, title: $"{token} one");
        await features.NewPostAsync(writer, title: $"{token} two");

        // Act
        var page = await Search(token, new Caller("an-admin", IsAdmin: true));

        // Assert
        Assert.Equal(2, page.TotalItems);
    }

    [Fact(DisplayName = "The title filter ignores case and treats the text as it is, wildcards included")]
    public async Task TitleFilterIsCaseInsensitiveAndLiteral()
    {
        // Arrange
        var token = Token();
        var writer = await features.NewAuthorAsync();
        var plain = await features.NewPostAsync(writer, publish: true, title: $"Plain {token}");
        var percent = await features.NewPostAsync(writer, publish: true, title: $"100% {token}");

        // Act
        var byCase = await Search(token.ToUpperInvariant(), writer.Caller);
        var byWildcard = await Search($"100% {token}", writer.Caller);
        var onlyThePercentSign = await Search($"% {token}", writer.Caller);

        // Assert
        Assert.Equivalent(new[] { plain.Id, percent.Id }, byCase.Items.Select(x => x.Id));
        Assert.Equal([percent.Id], byWildcard.Items.Select(x => x.Id));
        Assert.Equal([percent.Id], onlyThePercentSign.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "The status filter narrows the list, and never shows the drafts of others")]
    public async Task StatusFilterNarrowsTheList()
    {
        // Arrange
        var token = Token();
        var me = await features.NewAuthorAsync();
        var someone = await features.NewAuthorAsync();
        var published = await features.NewPostAsync(me, publish: true, title: $"{token} published");
        var draft = await features.NewPostAsync(me, title: $"{token} draft");
        await features.NewPostAsync(someone, title: $"{token} another draft");

        // Act
        var onlyDrafts = await Search(token, me.Caller, status: PostStatus.Draft);
        var onlyPublished = await Search(token, me.Caller, status: PostStatus.Published);

        // Assert
        Assert.Equal([draft.Id], onlyDrafts.Items.Select(x => x.Id));
        Assert.Equal([published.Id], onlyPublished.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "Posts can be ordered by title, and the newest come first by default")]
    public async Task OrderingByTitleAndDefaultOrder()
    {
        // Arrange
        var token = Token();
        var writer = await features.NewAuthorAsync();
        var bravo = await features.NewPostAsync(writer, publish: true, title: $"{token} Bravo");
        features.Time.Advance(TimeSpan.FromMinutes(1));
        var alpha = await features.NewPostAsync(writer, publish: true, title: $"{token} Alpha");

        // Act
        var byTitle = await Search(token, writer.Caller, orderBy: ["title"]);
        var byDefault = await Search(token, writer.Caller);

        // Assert
        Assert.Equal([alpha.Id, bravo.Id], byTitle.Items.Select(x => x.Id));
        Assert.Equal([alpha.Id, bravo.Id], byDefault.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "The list can be filtered by the handle of the author and ordered by it")]
    public async Task ListFiltersAndOrdersByAuthor()
    {
        // Arrange
        var token = Token();
        var first = await features.NewAuthorAsync("first");
        var second = await features.NewAuthorAsync("second");
        var byFirst = await features.NewPostAsync(first, publish: true, title: $"{token} Zulu");
        var bySecond = await features.NewPostAsync(second, publish: true, title: $"{token} Alpha");

        // Act
        var onlyFirst = await Search(token, first.Caller, author: first.Profile.Handle);
        var byAuthor = await Search(token, first.Caller, orderBy: ["author"]);
        var byAuthorDescending = await Search(token, first.Caller, orderBy: ["author:desc"]);

        // Assert
        Assert.Equal([byFirst.Id], onlyFirst.Items.Select(x => x.Id));
        Assert.Equal([byFirst.Id, bySecond.Id], byAuthor.Items.Select(x => x.Id));
        Assert.Equal([bySecond.Id, byFirst.Id], byAuthorDescending.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "Pages come with their totals")]
    public async Task PagesCarryTheirTotals()
    {
        // Arrange
        var token = Token();
        var writer = await features.NewAuthorAsync();

        for (var i = 0; i < 5; i++)
            await features.NewPostAsync(writer, publish: true, title: $"{token} {i}");

        // Act
        var page = await Search(token, writer.Caller, pageNumber: 2, pageSize: 2);

        // Assert
        Assert.Equal((2, 2, 5, 3, 2), (page.PageNumber, page.PageSize, page.TotalItems, page.TotalPages, page.Items.Count));
    }

    [Fact(DisplayName = "The creation date range is applied, with the end excluded")]
    public async Task CreationRangeIsApplied()
    {
        // Arrange
        var token = Token();
        var writer = await features.NewAuthorAsync();
        var early = await features.NewPostAsync(writer, publish: true, title: $"{token} early");
        features.Time.Advance(TimeSpan.FromDays(1));
        var boundary = features.Time.GetUtcNow();
        var late = await features.NewPostAsync(writer, publish: true, title: $"{token} late");

        // Act
        var before = await Search(token, writer.Caller, createdTo: boundary);
        var from = await Search(token, writer.Caller, createdFrom: boundary);

        // Assert
        Assert.Equal([early.Id], before.Items.Select(x => x.Id));
        Assert.Equal([late.Id], from.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "A field that cannot be ordered by is refused, naming it")]
    public async Task UnknownOrderingIsRefused()
    {
        // Arrange
        var request = new SearchPostsQuery(new SearchPostsRequest { OrderBy = ["body"] }, new Caller("someone", IsAdmin: false));

        // Act
        var result = await features.RunAsync<SearchPosts, SearchPostsQuery, Page<PostSummary>>(request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("orderBy", result.Error!.Details.Keys);
    }

    private static string Token()
    {
        return $"tok{Guid.NewGuid().ToString("N")[..10]}";
    }

    private async Task<Page<PostSummary>> Search(
        string title,
        Caller caller,
        PostStatus? status = null,
        string? author = null,
        string[]? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null,
        DateTimeOffset? createdFrom = null,
        DateTimeOffset? createdTo = null)
    {
        var filter = new SearchPostsRequest
        {
            Title = title,
            Status = status,
            Author = author,
            OrderBy = orderBy,
            PageNumber = pageNumber,
            PageSize = pageSize,
            CreatedFrom = createdFrom,
            CreatedTo = createdTo,
        };

        var result = await features.RunAsync<SearchPosts, SearchPostsQuery, Page<PostSummary>>(new SearchPostsQuery(filter, caller));

        Assert.True(result.IsSuccess, result.Error?.Message);

        return result.Value;
    }
}
