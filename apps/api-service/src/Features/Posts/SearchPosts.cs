using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Domain;
using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Pagination;

namespace Aurore.Blueprints.ApiService.Features.Posts;

/// <summary>What a client can ask of the list of posts: paging, ordering, date ranges, and these filters.</summary>
public sealed class SearchPostsRequest : TimestampPageRequest
{
    /// <summary>Posts whose title contains this text, without regard to case.</summary>
    public string? Title { get; init; }

    /// <summary>Only posts in this status. Drafts of other authors are never listed, whatever is asked.</summary>
    public PostStatus? Status { get; init; }

    /// <summary>Only posts written by the author with this handle.</summary>
    public string? Author { get; init; }
}

public sealed record SearchPostsQuery(SearchPostsRequest Filter, Caller Caller);

/// <summary>
/// Lists posts one page at a time: published posts for everyone, and also the caller's own drafts (all drafts for an admin).
/// Order by title, author, publishedAt or createdAt; the newest first by default.
/// </summary>
public sealed class SearchPosts(AppDbContext db, PaginationOptions pagination) : Feature<SearchPostsQuery, Page<PostSummary>>
{
    private static readonly SortMap<PostSummary> _sort = SortMap.For<PostSummary>()
        .Allow(x => x.Title)
        .Allow(x => x.AuthorHandle, "author")
        .Allow(x => x.PublishedAt)
        .DefaultTo(x => x.CreatedAt, SortDirection.Desc);

    protected override async ValueTask<Result<Page<PostSummary>>> ExecuteAsync(SearchPostsQuery request, CancellationToken cancellationToken)
    {
        var (filter, caller) = (request.Filter, request.Caller);

        var posts = db.Posts
            .VisibleTo(caller)
            .TitleContaining(filter.Title)
            .WrittenBy(filter.Author);

        if (filter.Status is { } status)
            posts = posts.Where(x => x.Status == status);

        var ranged = posts.WhereTimestamps(filter, x => x.CreatedAt, x => x.UpdatedAt);

        if (ranged.IsFailure)
            return ranged.Error!;

        return await ranged.Value.ToSummaries().ToPageAsync(filter, pagination, _sort, cancellationToken);
    }
}
