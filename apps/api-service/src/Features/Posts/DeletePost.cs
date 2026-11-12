using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Blueprints.ApiService.Features.Posts;

public sealed record DeletePostRequest(Guid PostId);

/// <summary>Deletes a post. The row stays, marked as deleted, so the slug is free again and nothing is lost.</summary>
public sealed class DeletePost(AppDbContext db) : Feature<DeletePostRequest>
{
    protected override async ValueTask<Result> ExecuteAsync(DeletePostRequest request, CancellationToken cancellationToken)
    {
        var post = await db.Posts.AsTracking().SingleOrDefaultAsync(x => x.Id == request.PostId, cancellationToken);

        if (post is null)
            return AppErrors.Posts.NotFound;

        db.Posts.Remove(post);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
