using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Extensions;
using FluentValidation;

namespace Aurore.Foundation.Core.Authentication.Validators;

public sealed class IdentityProviderOptionsValidator : AbstractValidator<IdentityProviderOptions>
{
    public IdentityProviderOptionsValidator(bool isProduction)
    {
        RuleFor(x => x.AuthorityUrl).Must(x => x.IsAbsoluteUrl())
            .WithMessage("'{PropertyValue}' is not an absolute URL.");

        RuleFor(x => x.AuthorityUrl).Must(x => x.IsLocalUrl() is false).When(_ => isProduction)
            .WithMessage("'{PropertyValue}' is a local address. Configure the production authority in appsettings.Production.json or environment variables.");

        // With RequireHttpsMetadata the JWT handler rejects an http authority, but only when the first token is validated.
        RuleFor(x => x.AuthorityUrl).Must(x => x.IsHttpsUrl())
            .When(x => x.AuthorityUrl.IsAbsoluteUrl() && (x.RequireHttpsMetadata || isProduction))
            .WithMessage("'{PropertyValue}' must use https when RequireHttpsMetadata is true or in production.");

        RuleFor(x => x.RequireHttpsMetadata).Equal(true).When(_ => isProduction)
            .WithMessage("RequireHttpsMetadata must be true in production.");

        RuleFor(x => x.Audience).NotEmpty();
        RuleFor(x => x.AuthorizationPath).NotEmpty();
        RuleFor(x => x.TokenPath).NotEmpty();
    }
}
