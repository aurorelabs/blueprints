using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Features.Comments;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;
using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.Pagination.Extensions;
using Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Resources.Posts;

/// <param name="Body">The text, up to 2000 characters.</param>
internal sealed record CreateCommentBody(string Body);

internal sealed class CreateCommentBodyValidator : RequestValidator<CreateCommentBody>
{
    public CreateCommentBodyValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

/// <summary>
/// The comments of a post as an infinite scroll, oldest first. Send the nextCursor of each response back as ?cursor= to get the
/// following page; change the ordering with ?orderBy=createdAt:desc.
/// </summary>
internal sealed class GetCommentsEndpoint : MinimalEndpoint<PostsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("{postId:guid}/comments", Handle)
            .RequireScopes("api.read")
            .ProducesCursorPage<CommentItem>();

        static async Task<Results<Ok<CursorPage<CommentItem>>, ProblemHttpResult>> Handle([AsParameters] ListCommentsRequest request, ListComments feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(request, cancellationToken);

            return result.ToHttpResult();
        }
    }
}

/// <summary>
/// Comments on a published post (409 on a draft). Needs an author profile; any author can, readers included. Send an
/// Idempotency-Key to be able to repeat the call safely.
/// </summary>
internal sealed class CreateCommentEndpoint : MinimalEndpoint<PostsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapPost("{postId:guid}/comments", Handle)
            .RequireScopes("api.write")
            .WithIdempotency();

        static async Task<Results<Created<CommentResponse>, ProblemHttpResult>> Handle(Guid postId, CreateCommentBody body, ClaimsPrincipal user, CreateComment feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(new CreateCommentRequest(user.ToCaller().Subject, postId, body.Body), cancellationToken);

            return result.ToHttpResult(comment => TypedResults.Created($"/v1/posts/{postId}/comments", comment));
        }
    }
}

/// <summary>Deletes a comment (admins only). The row is kept and marked as deleted.</summary>
internal sealed class DeleteCommentEndpoint : MinimalEndpoint<PostsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapDelete("{postId:guid}/comments/{commentId:guid}", Handle)
            .RequireRoles("admin")
            .RequireScopes("api.write");

        static async Task<Results<NoContent, ProblemHttpResult>> Handle(Guid postId, Guid commentId, DeleteComment feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(new DeleteCommentRequest(postId, commentId), cancellationToken);

            return result.ToHttpResult();
        }
    }
}
