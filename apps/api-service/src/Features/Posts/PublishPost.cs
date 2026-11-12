using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Domain;
using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Blueprints.ApiService.Features.Posts;

public sealed record PublishPostRequest(Guid PostId, Caller Caller);

/// <summary>
/// Makes a draft visible to everyone. Only the author of the post or an admin can do it, and a post that is already
/// published is a conflict, not a second publication.
/// </summary>
public sealed class PublishPost(AppDbContext db, TimeProvider time) : Feature<PublishPostRequest, PostResponse>
{
    protected override async ValueTask<Result<PostResponse>> ExecuteAsync(PublishPostRequest request, CancellationToken cancellationToken)
    {
        var post = await db.Posts.AsTracking().Include(x => x.Author).SingleOrDefaultAsync(x => x.Id == request.PostId, cancellationToken);

        if (post is null)
            return AppErrors.Posts.NotFound;

        if (request.Caller.IsAdmin is false && post.Author.Subject != request.Caller.Subject)
            return AppErrors.Posts.NotTheAuthor;

        if (post.Status is PostStatus.Published)
            return AppErrors.Posts.AlreadyPublished;

        post.Status = PostStatus.Published;
        post.PublishedAt = time.GetUtcNow();

        await db.SaveChangesAsync(cancellationToken);

        return new PostResponse(post.Id, post.Slug, post.Title, post.Body, post.Status, post.Author.Handle, post.PublishedAt, post.CreatedAt, post.UpdatedAt);
    }
}
