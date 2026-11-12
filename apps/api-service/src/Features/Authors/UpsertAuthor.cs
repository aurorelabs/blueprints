using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Blueprints.ApiService.Features.Authors;

/// <param name="Subject">The subject of the caller's identity, taken from the token.</param>
/// <param name="Handle">The public name wanted; it cannot change once the author exists.</param>
/// <param name="DisplayName">The name shown next to posts and comments.</param>
public sealed record UpsertAuthorRequest(string Subject, string Handle, string DisplayName);

public sealed record AuthorResponse(Guid Id, string Handle, string DisplayName, DateTimeOffset CreatedAt);

/// <summary>
/// Creates the author profile of the caller, or changes its display name when it exists: calling it again with the same data
/// changes nothing. A handle taken by another author is a conflict, answered by the unique violation decorator.
/// </summary>
public sealed class UpsertAuthor(AppDbContext db) : Feature<UpsertAuthorRequest, AuthorResponse>
{
    protected override async ValueTask<Result<AuthorResponse>> ExecuteAsync(UpsertAuthorRequest request, CancellationToken cancellationToken)
    {
        var author = await db.Authors.AsTracking().SingleOrDefaultAsync(x => x.Subject == request.Subject, cancellationToken);

        if (author is null)
        {
            author = new Author { Subject = request.Subject, Handle = request.Handle, DisplayName = request.DisplayName };
            db.Authors.Add(author);
        }
        else if (author.Handle != request.Handle)
        {
            return AppErrors.Authors.HandleCannotChange;
        }
        else
        {
            author.DisplayName = request.DisplayName;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AuthorResponse(author.Id, author.Handle, author.DisplayName, author.CreatedAt);
    }
}
