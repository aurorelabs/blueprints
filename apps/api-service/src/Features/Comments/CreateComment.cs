using System;
using System.Linq;
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

namespace Aurore.Blueprints.ApiService.Features.Comments;

/// <summary>The caller is identified by the subject of the token, never by the body of the request.</summary>
public sealed record CreateCommentRequest(string Subject, Guid PostId, string Body);

public sealed record CommentResponse(Guid Id, Guid PostId, string Body, string AuthorHandle, DateTimeOffset CreatedAt);

/// <summary>The business rule: only someone with an author profile can write.</summary>
internal sealed class CreateCommentAuthorValidator : FeatureValidator<CreateCommentRequest>
{
    public CreateCommentAuthorValidator(AppDbContext db)
    {
        RuleFor(x => x.Subject)
            .MustAsync((subject, cancellationToken) => db.Authors.AnyAsync(x => x.Subject == subject, cancellationToken))
            .WithMessage(AppErrors.Authors.ProfileRequired.Message);
    }

    public override ApplicationError? Error => AppErrors.Authors.ProfileRequired;
}

/// <summary>Adds a comment to a published post. A post that does not exist is a 404; one that is still a draft is a conflict.</summary>
public sealed class CreateComment(AppDbContext db) : Feature<CreateCommentRequest, CommentResponse>
{
    protected override async ValueTask<Result<CommentResponse>> ExecuteAsync(CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var post = await db.Posts
            .Where(x => x.Id == request.PostId)
            .Select(x => new { x.Id, x.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (post is null)
            return AppErrors.Posts.NotFound;

        if (post.Status is not PostStatus.Published)
            return AppErrors.Posts.NotPublished;

        var author = await db.Authors.SingleAsync(x => x.Subject == request.Subject, cancellationToken);
        var comment = new Comment { Body = request.Body, PostId = post.Id, AuthorId = author.Id };

        db.Comments.Add(comment);

        await db.SaveChangesAsync(cancellationToken);

        return new CommentResponse(comment.Id, comment.PostId, comment.Body, author.Handle, comment.CreatedAt);
    }
}
