using System;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Domain;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Features.Posts;
using Tests.Features.Support;

namespace Tests.Features.Posts;

[Trait("Feature", "Post update")]
public sealed class UpdatePostTests(FeaturesFixture features) : IClassFixture<FeaturesFixture>
{
    [Fact(DisplayName = "The author changes the title and the text, the slug stays and the update time moves")]
    public async Task AuthorUpdatesTheTextAndKeepsTheSlug()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author, publish: true);
        features.Time.Advance(TimeSpan.FromMinutes(10));

        // Act
        var result = await features.RunAsync<UpdatePost, UpdatePostRequest, PostResponse>(new UpdatePostRequest(post.Id, "A better title", "A better text.", author.Caller));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("A better title", result.Value.Title);
        Assert.Equal("A better text.", result.Value.Body);
        Assert.Equal(post.Slug, result.Value.Slug);
        Assert.Equal(post.Status, result.Value.Status);
        Assert.Equal(features.Time.GetUtcNow(), result.Value.UpdatedAt);
        Assert.Equal(post.CreatedAt, result.Value.CreatedAt);
    }

    [Fact(DisplayName = "Only the fields that are sent change: the title alone leaves the text, and the text alone leaves the title")]
    public async Task OnlyTheFieldsSentChange()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author, title: "Original title");

        // Act
        var titleOnly = await features.RunAsync<UpdatePost, UpdatePostRequest, PostResponse>(new UpdatePostRequest(post.Id, "New title", null, author.Caller));
        var bodyOnly = await features.RunAsync<UpdatePost, UpdatePostRequest, PostResponse>(new UpdatePostRequest(post.Id, null, "New text.", author.Caller));

        // Assert
        Assert.Equal(("New title", post.Body), (titleOnly.Value.Title, titleOnly.Value.Body));
        Assert.Equal(("New title", "New text."), (bodyOnly.Value.Title, bodyOnly.Value.Body));
    }

    [Fact(DisplayName = "An admin can change the post of someone else, another author cannot")]
    public async Task OnlyTheAuthorOrAnAdminUpdates()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var stranger = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author);

        // Act
        var refused = await features.RunAsync<UpdatePost, UpdatePostRequest, PostResponse>(new UpdatePostRequest(post.Id, "Hijacked", "Text", stranger.Caller));
        var byAdmin = await features.RunAsync<UpdatePost, UpdatePostRequest, PostResponse>(new UpdatePostRequest(post.Id, "Edited by an admin", "Text", new Caller("an-admin", IsAdmin: true)));

        // Assert
        Assert.Equal(AppErrors.Posts.NotTheAuthor, refused.Error);
        Assert.True(byAdmin.IsSuccess);
    }

    [Fact(DisplayName = "A post that does not exist or was deleted cannot be changed")]
    public async Task MissingAndDeletedPostsAreNotFound()
    {
        // Arrange
        var author = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(author);
        await features.RunAsync<DeletePost, DeletePostRequest>(new DeletePostRequest(post.Id));

        // Act
        var deleted = await features.RunAsync<UpdatePost, UpdatePostRequest, PostResponse>(new UpdatePostRequest(post.Id, "Title", "Text", author.Caller));
        var missing = await features.RunAsync<UpdatePost, UpdatePostRequest, PostResponse>(new UpdatePostRequest(Guid.NewGuid(), "Title", "Text", author.Caller));

        // Assert
        Assert.Equal(AppErrors.Posts.NotFound, deleted.Error);
        Assert.Equal(AppErrors.Posts.NotFound, missing.Error);
    }
}
