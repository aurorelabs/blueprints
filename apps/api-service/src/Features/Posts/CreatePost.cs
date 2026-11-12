using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Core.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Blueprints.ApiService.Features.Posts;

/// <summary>The caller is identified by the subject of the token, never by the body of the request.</summary>
public sealed record CreatePostRequest(string Subject, string Slug, string Title, string Body);

/// <summary>The business rule: only someone with an author profile can write.</summary>
internal sealed class CreatePostAuthorValidator : FeatureValidator<CreatePostRequest>
{
    public CreatePostAuthorValidator(AppDbContext db)
    {
        RuleFor(x => x.Subject)
            .MustAsync((subject, cancellationToken) => db.Authors.AnyAsync(x => x.Subject == subject, cancellationToken))
            .WithMessage(AppErrors.Authors.ProfileRequired.Message);
    }

    public override ApplicationError? Error => AppErrors.Authors.ProfileRequired;
}

/// <summary>Writes a new post as a draft. A slug already in use is a conflict, answered by the unique violation decorator.</summary>
public sealed class CreatePost(AppDbContext db) : Feature<CreatePostRequest, PostResponse>
{
    protected override async ValueTask<Result<PostResponse>> ExecuteAsync(CreatePostRequest request, CancellationToken cancellationToken)
    {
        var author = await db.Authors.SingleAsync(x => x.Subject == request.Subject, cancellationToken);
        var post = new Post { Slug = request.Slug, Title = request.Title, Body = request.Body, AuthorId = author.Id };

        db.Posts.Add(post);

        await db.SaveChangesAsync(cancellationToken);

        return new PostResponse(post.Id, post.Slug, post.Title, post.Body, post.Status, author.Handle, post.PublishedAt, post.CreatedAt, post.UpdatedAt);
    }
}
