using Aurore.Foundation.Core.Entities;

namespace Aurore.Blueprints.ApiService.Core.Domain.Entities;

/// <summary>
/// An account that can write posts and comments, linked to the identity that signed in. Having one does not oblige the owner to
/// write anything.
/// </summary>
public sealed class Author : Entity
{
    /// <summary>The subject (the sub claim) of the identity this author belongs to. Never changes.</summary>
    public required string Subject { get; init; }

    /// <summary>The public name in lower case ("ada-lovelace"). Unique, and it never changes once chosen.</summary>
    public required string Handle { get; init; }

    /// <summary>The name shown next to the posts and comments.</summary>
    public required string DisplayName { get; set; }
}
