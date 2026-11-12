using System;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Domain;
using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Features.Posts;
using Microsoft.EntityFrameworkCore;
using Tests.Features.Support;

namespace Tests.Features.Posts;

[Trait("Feature", "Posts")]
public sealed class PostsTests(FeaturesFixture features) : IClassFixture<FeaturesFixture>
{
    public static TheoryData<string, bool> DraftVisibility => new()
    {
        { "author", true },
        { "another author", false },
        { "admin", true },
    };

    [Fact(DisplayName = "Someone without an author profile cannot write a post")]
    public async Task PostNeedsAnAuthorProfile()
    {
        // Arrange
        var request = new CreatePostRequest($"subject-{Guid.NewGuid():N}", $"post-{Guid.NewGuid():N}", "Title", "Text");

        // Act
        var result = await features.RunAsync<CreatePost, CreatePostRequest, PostResponse>(request);

        // Assert
        Assert.Equal(AppErrors.Authors.ProfileRequired.Code, result.Error!.Code);
        Assert.Equal(AppErrors.Authors.ProfileRequired.Kind, result.Error.Kind);
    }

    [Fact(DisplayName = "A new post is a draft written by the author")]
    public async Task NewPostIsADraft()
    {
        // Arrange
        var author = await features.NewAuthorAsync();

        // Act
        var post = await features.NewPostAsync(author);

        // Assert
        Assert.Equal(PostStatus.Draft, post.Status);
        Assert.Null(post.PublishedAt);
        Assert.Equal(author.Profile.Handle, post.AuthorHandle);
    }

    [Fact(DisplayName = "A slug that is already used is a conflict, and a deleted post gives its slug back")]
    public async Task SlugIsUniqueAmongPostsThatExist()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var first = await features.NewPostAsync(author);
        var again = new CreatePostRequest(author.Caller.Subject, first.Slug, "Other title", "Other text");

        // Act
        var duplicate = await features.RunAsync<CreatePost, CreatePostRequest, PostResponse>(again);
        await features.RunAsync<DeletePost, DeletePostRequest>(new DeletePostRequest(first.Id));
        var afterDeleting = await features.RunAsync<CreatePost, CreatePostRequest, PostResponse>(again);

        // Assert
        Assert.Equal(AppErrors.Posts.SlugTaken, duplicate.Error);
        Assert.True(afterDeleting.IsSuccess);
    }

    [Theory(DisplayName = "A draft is visible to its author and to admins, and does not exist for anyone else")]
    [MemberData(nameof(DraftVisibility))]
    public async Task DraftIsVisibleToItsAuthorAndAdminsOnly(string who, bool visible)
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var other = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author);
        var caller = who switch
        {
            "author" => author.Caller,
            "another author" => other.Caller,
            _ => new Caller("an-admin", IsAdmin: true),
        };

        // Act
        var result = await features.RunAsync<GetPost, GetPostRequest, PostResponse>(new GetPostRequest(post.Slug, caller));

        // Assert
        Assert.Equal(visible, result.IsSuccess);
        Assert.True(visible || result.Error == AppErrors.Posts.NotFound);
    }

    [Fact(DisplayName = "A published post is visible to everyone")]
    public async Task PublishedPostIsVisibleToEveryone()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author, publish: true);

        // Act
        var result = await features.RunAsync<GetPost, GetPostRequest, PostResponse>(new GetPostRequest(post.Slug, new Caller("a-stranger", IsAdmin: false)));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(post.Id, result.Value.Id);
    }

    [Fact(DisplayName = "A post that does not exist is a not found")]
    public async Task UnknownPostIsNotFound()
    {
        // Arrange
        var request = new GetPostRequest("no-such-post", new Caller("someone", IsAdmin: true));

        // Act
        var result = await features.RunAsync<GetPost, GetPostRequest, PostResponse>(request);

        // Assert
        Assert.Equal(AppErrors.Posts.NotFound, result.Error);
    }

    [Fact(DisplayName = "The author publishes a draft, stamped with the time of the publication")]
    public async Task AuthorPublishesADraft()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author);
        features.Time.Advance(TimeSpan.FromHours(2));

        // Act
        var result = await features.RunAsync<PublishPost, PublishPostRequest, PostResponse>(new PublishPostRequest(post.Id, author.Caller));

        // Assert
        Assert.Equal(PostStatus.Published, result.Value.Status);
        Assert.Equal(features.Time.GetUtcNow(), result.Value.PublishedAt);
    }

    [Fact(DisplayName = "An admin can publish the draft of someone else, another author cannot")]
    public async Task OnlyTheAuthorOrAnAdminPublishes()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var stranger = await features.NewAuthorAsync();
        var first = await features.NewPostAsync(author);
        var second = await features.NewPostAsync(author);

        // Act
        var refused = await features.RunAsync<PublishPost, PublishPostRequest, PostResponse>(new PublishPostRequest(first.Id, stranger.Caller));
        var byAdmin = await features.RunAsync<PublishPost, PublishPostRequest, PostResponse>(new PublishPostRequest(second.Id, new Caller("an-admin", IsAdmin: true)));

        // Assert
        Assert.Equal(AppErrors.Posts.NotTheAuthor, refused.Error);
        Assert.True(byAdmin.IsSuccess);
    }

    [Fact(DisplayName = "A post cannot be published twice, and one that does not exist cannot be published")]
    public async Task PublishingTwiceOrNothingFails()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author, publish: true);

        // Act
        var twice = await features.RunAsync<PublishPost, PublishPostRequest, PostResponse>(new PublishPostRequest(post.Id, author.Caller));
        var missing = await features.RunAsync<PublishPost, PublishPostRequest, PostResponse>(new PublishPostRequest(Guid.NewGuid(), author.Caller));

        // Assert
        Assert.Equal(AppErrors.Posts.AlreadyPublished, twice.Error);
        Assert.Equal(AppErrors.Posts.NotFound, missing.Error);
    }

    [Fact(DisplayName = "Deleting a post keeps its row marked as deleted and hides it from reads")]
    public async Task DeleteKeepsTheRow()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author, publish: true);

        // Act
        var deleted = await features.RunAsync<DeletePost, DeletePostRequest>(new DeletePostRequest(post.Id));
        var read = await features.RunAsync<GetPost, GetPostRequest, PostResponse>(new GetPostRequest(post.Slug, author.Caller));
        var row = await features.QueryAsync(db => db.Posts.IgnoreQueryFilters().SingleAsync(x => x.Id == post.Id));

        // Assert
        Assert.True(deleted.IsSuccess);
        Assert.Equal(AppErrors.Posts.NotFound, read.Error);
        Assert.NotNull(row.DeletedAt);
    }

    [Fact(DisplayName = "Deleting a post that is already deleted or does not exist is a not found")]
    public async Task DeletingTwiceIsNotFound()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author);
        await features.RunAsync<DeletePost, DeletePostRequest>(new DeletePostRequest(post.Id));

        // Act
        var again = await features.RunAsync<DeletePost, DeletePostRequest>(new DeletePostRequest(post.Id));

        // Assert
        Assert.Equal(AppErrors.Posts.NotFound, again.Error);
    }
}
