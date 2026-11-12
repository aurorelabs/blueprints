using System;
using Aurore.Blueprints.ApiService.Core.Domain.Entities;

namespace Aurore.Blueprints.ApiService.Features.Posts;

/// <summary>One post in full.</summary>
public sealed record PostResponse(
    Guid Id,
    string Slug,
    string Title,
    string Body,
    PostStatus Status,
    string AuthorHandle,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// One post in a list: everything but the body. Declared with init properties and filled with an object initializer, not a
/// constructor: the page is ordered after this projection, and the database can only find a property of an initializer (a
/// property of a constructor call cannot be translated).
/// </summary>
public sealed record PostSummary
{
    public required Guid Id { get; init; }

    public required string Slug { get; init; }

    public required string Title { get; init; }

    public required PostStatus Status { get; init; }

    public required string AuthorHandle { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
