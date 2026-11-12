using System;
using Aurore.Foundation.Core.Entities;

namespace Aurore.Blueprints.ApiService.Core.Domain.Entities;

/// <summary>A comment an author left on a published post. Deleting it keeps the row, marked as deleted.</summary>
public sealed class Comment : SoftDeletableEntity
{
    public required string Body { get; set; }

    public Guid PostId { get; init; }

    public Post Post { get; init; } = null!;

    public Guid AuthorId { get; init; }

    public Author Author { get; init; } = null!;
}
