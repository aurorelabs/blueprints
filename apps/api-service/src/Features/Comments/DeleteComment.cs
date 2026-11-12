using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Blueprints.ApiService.Features.Comments;

public sealed record DeleteCommentRequest(Guid PostId, Guid CommentId);

/// <summary>Deletes a comment of a post (the row stays, marked as deleted). A comment asked for under another post does not exist.</summary>
public sealed class DeleteComment(AppDbContext db) : Feature<DeleteCommentRequest>
{
    protected override async ValueTask<Result> ExecuteAsync(DeleteCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await db.Comments.AsTracking().SingleOrDefaultAsync(x => x.Id == request.CommentId && x.PostId == request.PostId, cancellationToken);

        if (comment is null)
            return AppErrors.Comments.NotFound;

        db.Comments.Remove(comment);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
