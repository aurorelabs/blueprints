using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Features.Posts;
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

/// <summary>Posts and their comments. Anyone signed in reads, authors comment, editors write, admins delete.</summary>
internal sealed class PostsResource : ResourceGroup;

/// <param name="Slug">The readable name in the address: lower case letters and digits separated by single hyphens. Unique.</param>
/// <param name="Title">The title, up to 160 characters.</param>
/// <param name="Body">The text, up to 20000 characters.</param>
internal sealed record CreatePostBody(string Slug, string Title, string Body);

internal sealed class CreatePostBodyValidator : RequestValidator<CreatePostBody>
{
    public CreatePostBodyValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().Length(3, 80).Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Use lower case letters and digits, separated by single hyphens.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(20000);
    }
}

/// <summary>
/// Lists posts one page at a time. Try: ?title=hello, ?status=Draft, ?orderBy=title, ?pageNumber=2&amp;pageSize=5,
/// ?createdFrom=2026-01-01T00:00:00Z. Drafts only show for their author and for admins.
/// </summary>
internal sealed class GetPostsEndpoint : MinimalEndpoint<PostsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet(string.Empty, Handle)
            .RequireScopes("api.read")
            .ProducesPage<PostSummary>();

        static async Task<Results<Ok<Page<PostSummary>>, ProblemHttpResult>> Handle([AsParameters] SearchPostsRequest request, ClaimsPrincipal user, SearchPosts feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(new SearchPostsQuery(request, user.ToCaller()), cancellationToken);

            return result.ToHttpResult();
        }
    }
}

internal sealed class GetPostEndpoint : MinimalEndpoint<PostsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("{slug}", Handle)
            .RequireScopes("api.read");

        static async Task<Results<Ok<PostResponse>, ProblemHttpResult>> Handle(string slug, ClaimsPrincipal user, GetPost feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(new GetPostRequest(slug, user.ToCaller()), cancellationToken);

            return result.ToHttpResult();
        }
    }
}

/// <summary>
/// Writes a draft. Needs the editor or admin role and an author profile; the slug must be free (409 otherwise). Send an
/// Idempotency-Key to be able to repeat the call safely.
/// </summary>
internal sealed class CreatePostEndpoint : MinimalEndpoint<PostsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapPost(string.Empty, Handle)
            .RequireAnyRole("editor", "admin")
            .RequireScopes("api.write")
            .WithIdempotency();

        static async Task<Results<Created<PostResponse>, ProblemHttpResult>> Handle(CreatePostBody body, ClaimsPrincipal user, CreatePost feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(new CreatePostRequest(user.ToCaller().Subject, body.Slug, body.Title, body.Body), cancellationToken);

            return result.ToHttpResult(post => TypedResults.Created($"/v1/posts/{post.Slug}", post));
        }
    }
}

/// <summary>Makes a draft visible to everyone. Only its author or an admin can, and only once (409 after that).</summary>
internal sealed class PublishPostEndpoint : MinimalEndpoint<PostsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapPost("{id:guid}/publish", Handle)
            .RequireAnyRole("editor", "admin")
            .RequireScopes("api.write");

        static async Task<Results<Ok<PostResponse>, ProblemHttpResult>> Handle(Guid id, ClaimsPrincipal user, PublishPost feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(new PublishPostRequest(id, user.ToCaller()), cancellationToken);

            return result.ToHttpResult();
        }
    }
}

/// <summary>Deletes a post (admins only). The row is kept and marked, so the slug is free again.</summary>
internal sealed class DeletePostEndpoint : MinimalEndpoint<PostsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapDelete("{id:guid}", Handle)
            .RequireRoles("admin")
            .RequireScopes("api.write");

        static async Task<Results<NoContent, ProblemHttpResult>> Handle(Guid id, DeletePost feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(new DeletePostRequest(id), cancellationToken);

            return result.ToHttpResult();
        }
    }
}

/// <param name="Title">The new title, up to 160 characters; leave it out to keep the current one.</param>
/// <param name="Body">The new text, up to 20000 characters; leave it out to keep the current one.</param>
internal sealed record PatchPostBody(string? Title, string? Body);

internal sealed class PatchPostBodyValidator : RequestValidator<PatchPostBody>
{
    public PatchPostBodyValidator()
    {
        RuleFor(x => x).Must(x => x.Title is not null || x.Body is not null).WithMessage("Send the title, the body or both.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160).When(x => x.Title is not null);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(20000).When(x => x.Body is not null);
    }
}

/// <summary>
/// Changes the fields of a post that are sent, and only those (a PATCH: the slug, the status and what is left out stay as they are). Only its
/// author or an admin can.
/// </summary>
internal sealed class PatchPostEndpoint : MinimalEndpoint<PostsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapPatch("{id:guid}", Handle)
            .RequireAnyRole("editor", "admin")
            .RequireScopes("api.write");

        static async Task<Results<Ok<PostResponse>, ProblemHttpResult>> Handle(Guid id, PatchPostBody body, ClaimsPrincipal user, UpdatePost feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(new UpdatePostRequest(id, body.Title, body.Body, user.ToCaller()), cancellationToken);

            return result.ToHttpResult();
        }
    }
}
