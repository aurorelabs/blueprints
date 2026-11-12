using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Pagination;

namespace Aurore.Blueprints.ApiService.Features.Posts;

/// <summary>The feed: published posts as an infinite scroll. Send the nextCursor of each response back to get the following page.</summary>
public sealed class StreamPostsRequest : TimestampCursorRequest
{
    /// <summary>Posts whose title contains this text, without regard to case.</summary>
    public string? Title { get; init; }

    /// <summary>Only posts written by the author with this handle.</summary>
    public string? Author { get; init; }
}

/// <summary>
/// The published posts, newest first, as an infinite scroll (a cursor, no totals, and the same cost on every page). Drafts are never in
/// the feed, not even the caller's own. Order by title, author or createdAt. A cursor cannot order by publishedAt, because a
/// comparison with a missing value never matches.
/// </summary>
public sealed class StreamPosts(AppDbContext db, PaginationOptions pagination) : Feature<StreamPostsRequest, CursorPage<PostSummary>>
{
    private static readonly SortMap<PostSummary> _sort = SortMap.For<PostSummary>()
        .Allow(x => x.Title)
        .Allow(x => x.AuthorHandle, "author")
        .DefaultTo(x => x.CreatedAt, SortDirection.Desc);

    protected override async ValueTask<Result<CursorPage<PostSummary>>> ExecuteAsync(StreamPostsRequest request, CancellationToken cancellationToken)
    {
        var posts = db.Posts
            .Where(x => x.Status == PostStatus.Published)
            .TitleContaining(request.Title)
            .WrittenBy(request.Author);

        var ranged = posts.WhereTimestamps(request, x => x.CreatedAt, x => x.UpdatedAt);

        if (ranged.IsFailure)
            return ranged.Error!;

        return await ranged.Value.ToSummaries().ToCursorPageAsync(request, pagination, _sort, cancellationToken);
    }
}
