using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Features.Authors;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;
using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.Core.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Resources.Authors;

/// <summary>The author profiles: the account that writes posts and comments.</summary>
internal sealed class AuthorsResource : ResourceGroup;

/// <summary>The profile the caller wants.</summary>
/// <param name="Handle">The public name: lower case letters, digits and hyphens. It cannot change once chosen.</param>
/// <param name="DisplayName">The name shown next to posts and comments.</param>
internal sealed record PutAuthorBody(string Handle, string DisplayName);

internal sealed class PutAuthorBodyValidator : RequestValidator<PutAuthorBody>
{
    public PutAuthorBodyValidator()
    {
        RuleFor(x => x.Handle).NotEmpty().Length(3, 32).Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Use lower case letters and digits, separated by single hyphens.");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(80);
    }
}

/// <summary>
/// Creates the profile of the caller, or changes its display name. Calling it again with the same data changes nothing, which is
/// why it is a PUT. The handle belongs to one author for good.
/// </summary>
internal sealed class PutMyAuthorEndpoint : MinimalEndpoint<AuthorsResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapPut("me", Handle)
            .RequireScopes("api.write");

        static async Task<Results<Ok<AuthorResponse>, ProblemHttpResult>> Handle(PutAuthorBody body, ClaimsPrincipal user, UpsertAuthor feature, CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(new UpsertAuthorRequest(user.ToCaller().Subject, body.Handle, body.DisplayName), cancellationToken);

            return result.ToHttpResult();
        }
    }
}
