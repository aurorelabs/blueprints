using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Features.Comments;
using Aurore.Blueprints.ApiService.Features.Posts;
using Aurore.Foundation.Core.Pagination.Models;
using Tests.Features.Support;

namespace Tests.Features.Comments;

[Trait("Feature", "Comments")]
public sealed class CommentsTests(FeaturesFixture features) : IClassFixture<FeaturesFixture>
{
    [Fact(DisplayName = "Someone without an author profile cannot comment")]
    public async Task CommentNeedsAnAuthorProfile()
    {
        // Arrange
        var writer = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(writer, publish: true);

        // Act
        var result = await features.RunAsync<CreateComment, CreateCommentRequest, CommentResponse>(new CreateCommentRequest($"subject-{Guid.NewGuid():N}", post.Id, "Nice."));

        // Assert
        Assert.Equal(AppErrors.Authors.ProfileRequired.Code, result.Error!.Code);
    }

    [Fact(DisplayName = "An author comments on a published post, and the comment carries the handle")]
    public async Task AuthorCommentsOnAPublishedPost()
    {
        // Arrange
        var writer = await features.NewAuthorAsync();
        var reader = await features.NewAuthorAsync("reader");
        var post = await features.NewPostAsync(writer, publish: true);

        // Act
        var result = await features.RunAsync<CreateComment, CreateCommentRequest, CommentResponse>(new CreateCommentRequest(reader.Caller.Subject, post.Id, "Nice."));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(post.Id, result.Value.PostId);
        Assert.Equal(reader.Profile.Handle, result.Value.AuthorHandle);
    }

    [Fact(DisplayName = "A draft cannot be commented on, and a post that does not exist is a not found")]
    public async Task DraftAndMissingPostsRefuseComments()
    {
        // Arrange
        var writer = await features.NewAuthorAsync();
        var draft = await features.NewPostAsync(writer);

        // Act
        var onDraft = await features.RunAsync<CreateComment, CreateCommentRequest, CommentResponse>(new CreateCommentRequest(writer.Caller.Subject, draft.Id, "Early."));
        var onNothing = await features.RunAsync<CreateComment, CreateCommentRequest, CommentResponse>(new CreateCommentRequest(writer.Caller.Subject, Guid.NewGuid(), "Lost."));

        // Assert
        Assert.Equal(AppErrors.Posts.NotPublished, onDraft.Error);
        Assert.Equal(AppErrors.Posts.NotFound, onNothing.Error);
    }

    [Fact(DisplayName = "Following the cursor lists every comment of the post once, oldest first, and no comment of other posts")]
    public async Task CursorListsTheCommentsOfThePost()
    {
        // Arrange
        var writer = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(writer, publish: true);
        var other = await features.NewPostAsync(writer, publish: true);
        var written = new List<Guid>();

        for (var i = 0; i < 5; i++)
        {
            features.Time.Advance(TimeSpan.FromSeconds(1));
            written.Add((await features.RunAsync<CreateComment, CreateCommentRequest, CommentResponse>(new CreateCommentRequest(writer.Caller.Subject, post.Id, $"Comment {i}"))).Value.Id);
        }

        await features.RunAsync<CreateComment, CreateCommentRequest, CommentResponse>(new CreateCommentRequest(writer.Caller.Subject, other.Id, "Elsewhere"));

        // Act
        var seen = new List<Guid>();
        string? cursor = null;

        do
        {
            var result = await features.RunAsync<ListComments, ListCommentsRequest, CursorPage<CommentItem>>(new ListCommentsRequest { PostId = post.Id, PageSize = 2, Cursor = cursor });

            Assert.True(result.IsSuccess, result.Error?.Message);
            seen.AddRange(result.Value.Items.Select(x => x.Id));
            cursor = result.Value.NextCursor;
        }
        while (cursor is not null);

        // Assert
        Assert.Equal(written, seen);
    }

    [Fact(DisplayName = "A cursor of one post is refused on another post")]
    public async Task CursorBelongsToItsPost()
    {
        // Arrange
        var writer = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(writer, publish: true);
        var other = await features.NewPostAsync(writer, publish: true);

        foreach (var i in Enumerable.Range(0, 3))
            await features.RunAsync<CreateComment, CreateCommentRequest, CommentResponse>(new CreateCommentRequest(writer.Caller.Subject, post.Id, $"Comment {i}"));

        var first = await features.RunAsync<ListComments, ListCommentsRequest, CursorPage<CommentItem>>(new ListCommentsRequest { PostId = post.Id, PageSize = 1 });

        // Act
        var onAnotherPost = await features.RunAsync<ListComments, ListCommentsRequest, CursorPage<CommentItem>>(new ListCommentsRequest { PostId = other.Id, PageSize = 1, Cursor = first.Value.NextCursor });

        // Assert
        Assert.True(onAnotherPost.IsFailure);
    }

    [Fact(DisplayName = "The comments of a post that does not exist or was deleted are a not found")]
    public async Task CommentsOfAMissingOrDeletedPostAreNotFound()
    {
        // Arrange
        var writer = await features.NewAuthorAsync();
        var post = await features.NewPostAsync(writer, publish: true);
        await features.RunAsync<DeletePost, DeletePostRequest>(new DeletePostRequest(post.Id));

        // Act
        var deleted = await features.RunAsync<ListComments, ListCommentsRequest, CursorPage<CommentItem>>(new ListCommentsRequest { PostId = post.Id });
        var missing = await features.RunAsync<ListComments, ListCommentsRequest, CursorPage<CommentItem>>(new ListCommentsRequest { PostId = Guid.NewGuid() });

        // Assert
        Assert.Equal(AppErrors.Posts.NotFound, deleted.Error);
        Assert.Equal(AppErrors.Posts.NotFound, missing.Error);
    }
}
