using System;
using System.Collections.Generic;
using Aurore.Foundation.Core.Entities;

namespace Aurore.Blueprints.ApiService.Core.Domain.Entities;

/// <summary>An article written by an author. Deleting it keeps the row, marked as deleted.</summary>
public sealed class Post : SoftDeletableEntity
{
    /// <summary>The unique, readable name in the address of the post ("hello-world").</summary>
    public required string Slug { get; set; }

    public required string Title { get; set; }

    public required string Body { get; set; }

    public PostStatus Status { get; set; } = PostStatus.Draft;

    /// <summary>When the post was published; null while it is a draft.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    public Guid AuthorId { get; init; }

    public Author Author { get; init; } = null!;

    public ICollection<Comment> Comments { get; init; } = [];
}
