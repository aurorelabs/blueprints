using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Features.Posts;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;
using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Resources.Feed;

/// <summary>The published posts as an infinite scroll.</summary>
internal sealed class FeedResource : ResourceGroup;

/// <summary>
/// The feed: published posts, newest first. Send the nextCursor of each response back as ?cursor= to get the following page. Try ?author=bob,
/// ?title=hello, ?orderBy=title, ?pageSize=5. Drafts are never in the feed.
/// </summary>
internal sealed class GetFeedEndpoint : MinimalEndpoint<FeedResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet(string.Empty, Handle)
            .RequireScopes("api.read")
            .ProducesCursorPage<PostSummary>();

        static async Task<Results<Ok<CursorPage<PostSummary>>, ProblemHttpResult>> Handle([AsParameters] StreamPostsRequest request, StreamPosts feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(request, cancellationToken);

            return result.ToHttpResult();
        }
    }
}
