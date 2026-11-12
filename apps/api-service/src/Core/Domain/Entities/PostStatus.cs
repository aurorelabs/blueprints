namespace Aurore.Blueprints.ApiService.Core.Domain.Entities;

/// <summary>Where a post is in its life.</summary>
public enum PostStatus
{
    /// <summary>Visible to its author and to admins only; it cannot be commented on.</summary>
    Draft,

    /// <summary>Visible to everyone, and open for comments.</summary>
    Published,
}
