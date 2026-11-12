using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Pagination;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Blueprints.ApiService.Features.Comments;

/// <summary>One comment in a list. Filled with an object initializer, see <see cref="Posts.PostSummary"/>.</summary>
public sealed record CommentItem
{
    public required Guid Id { get; init; }

    public required string Body { get; init; }

    public required string AuthorHandle { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>The comments of a post as an infinite scroll: send the nextCursor of each response back to get the following page.</summary>
public sealed class ListCommentsRequest : TimestampCursorRequest
{
    /// <summary>The post, from the address. It is part of the cursor, so a cursor cannot be used on another post.</summary>
    [FromRoute]
    public Guid PostId { get; init; }
}

/// <summary>Lists the comments of a post, oldest first unless asked otherwise (orderBy=createdAt:desc).</summary>
public sealed class ListComments(AppDbContext db, PaginationOptions pagination) : Feature<ListCommentsRequest, CursorPage<CommentItem>>
{
    private static readonly SortMap<CommentItem> _sort = SortMap.For<CommentItem>()
        .DefaultTo(x => x.CreatedAt, SortDirection.Asc);

    protected override async ValueTask<Result<CursorPage<CommentItem>>> ExecuteAsync(ListCommentsRequest request, CancellationToken cancellationToken)
    {
        if (await db.Posts.AnyAsync(x => x.Id == request.PostId, cancellationToken) is false)
            return AppErrors.Posts.NotFound;

        var ranged = db.Comments
            .Where(x => x.PostId == request.PostId)
            .WhereTimestamps(request, x => x.CreatedAt, x => x.UpdatedAt);

        if (ranged.IsFailure)
            return ranged.Error!;

        return await ranged.Value
            .Select(x => new CommentItem { Id = x.Id, Body = x.Body, AuthorHandle = x.Author.Handle, CreatedAt = x.CreatedAt })
            .ToCursorPageAsync(request, pagination, _sort, cancellationToken);
    }
}
