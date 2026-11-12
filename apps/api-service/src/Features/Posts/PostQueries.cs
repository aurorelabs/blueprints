using System.Linq;
using Aurore.Blueprints.ApiService.Core.Domain;
using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Blueprints.ApiService.Features.Posts;

/// <summary>The pieces of query the post lists have in common, so the rules of who sees what are written once.</summary>
internal static class PostQueries
{
    /// <summary>Published posts for everyone, and also the drafts of the caller (all the drafts for an admin).</summary>
    public static IQueryable<Post> VisibleTo(this IQueryable<Post> posts, Caller caller)
    {
        return posts.Where(x => x.Status == PostStatus.Published || caller.IsAdmin || x.Author.Subject == caller.Subject);
    }

    /// <summary>Posts whose title contains the text, without regard to case. The text is searched for as it is, wildcards included.</summary>
    public static IQueryable<Post> TitleContaining(this IQueryable<Post> posts, string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return posts;

        var pattern = $"%{Escape(title)}%";

        return posts.Where(x => EF.Functions.ILike(x.Title, pattern, "\\"));
    }

    /// <summary>Posts written by the author with that handle.</summary>
    public static IQueryable<Post> WrittenBy(this IQueryable<Post> posts, string? handle)
    {
        return string.IsNullOrWhiteSpace(handle) ? posts : posts.Where(x => x.Author.Handle == handle);
    }

    /// <summary>The list item of a post. An object initializer, see <see cref="PostSummary"/>.</summary>
    public static IQueryable<PostSummary> ToSummaries(this IQueryable<Post> posts)
    {
        return posts.Select(x => new PostSummary
        {
            Id = x.Id,
            Slug = x.Slug,
            Title = x.Title,
            Status = x.Status,
            AuthorHandle = x.Author.Handle,
            PublishedAt = x.PublishedAt,
            CreatedAt = x.CreatedAt,
        });
    }

    // LIKE treats % and _ as wildcards; the user's text is searched for as it is.
    private static string Escape(string text)
    {
        return text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }
}
