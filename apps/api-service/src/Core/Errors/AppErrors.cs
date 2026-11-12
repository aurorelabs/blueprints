using Aurore.Foundation.Core.Errors;

namespace Aurore.Blueprints.ApiService.Core.Errors;

/// <summary>
/// The single place for this application's errors. The standard ones (AppErrors.NotFound, AppErrors.Conflict, ...) are
/// inherited from the foundation; business errors are grouped in nested classes, one line each. Codes read
/// "area.snake_case_name" and are what clients rely on; messages can change freely.
/// </summary>
public sealed class AppErrors : StandardErrors
{
    private AppErrors()
    {
    }

    public static class Authors
    {
        public static readonly ApplicationError ProfileRequired =
            ApplicationError.Conflict("author.profile_required", "Create your author profile first (PUT /v1/authors/me).");

        public static readonly ApplicationError HandleTaken =
            ApplicationError.Conflict("author.handle_taken", "That handle belongs to another author.");

        public static readonly ApplicationError HandleCannotChange =
            ApplicationError.Conflict("author.handle_cannot_change", "The handle of an author never changes.");
    }

    public static class Comments
    {
        public static readonly ApplicationError NotFound =
            ApplicationError.NotFound("comment.not_found", "There is no such comment on that post.");
    }

    public static class Posts
    {
        public static readonly ApplicationError NotFound =
            ApplicationError.NotFound("post.not_found", "There is no such post.");

        public static readonly ApplicationError SlugTaken =
            ApplicationError.Conflict("post.slug_taken", "Another post already uses that slug.");

        public static readonly ApplicationError AlreadyPublished =
            ApplicationError.Conflict("post.already_published", "The post is already published.");

        public static readonly ApplicationError NotPublished =
            ApplicationError.Conflict("post.not_published", "Only published posts can be commented on.");

        public static readonly ApplicationError NotTheAuthor =
            ApplicationError.Forbidden("post.not_the_author", "Only the author of the post, or an admin, can do this.");
    }
}
