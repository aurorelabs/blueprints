using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.HttpDependencies.Models;
using FluentValidation;

namespace Aurore.Foundation.Core.HttpDependencies.Validators;

public sealed class HttpDependencyOptionsValidator : AbstractValidator<HttpDependencyOptions>
{
    public HttpDependencyOptionsValidator(bool isProduction)
    {
        RuleFor(x => x.BaseUrl).Must(x => x.IsAbsoluteUrl())
            .WithMessage("'{PropertyValue}' is not an absolute URL.");

        RuleFor(x => x.BaseUrl).Must(x => x.IsLocalUrl() is false).When(_ => isProduction)
            .WithMessage("'{PropertyValue}' is a local address. Configure the production URL in appsettings.Production.json or environment variables.");

        // Optional sections: FluentValidation skips child rules while the section is null.
        RuleFor(x => x.ApiKey!).ChildRules(apiKey =>
        {
            apiKey.RuleFor(x => x.HeaderName).NotEmpty();
            apiKey.RuleFor(x => x.Value).NotEmpty().WithMessage("The API key value is empty. Supply it through environment variables or a secret store.");
        });

        RuleFor(x => x.ClientCredentials!).ChildRules(credentials =>
        {
            credentials.RuleFor(x => x.TokenUrl).Must(x => x.IsAbsoluteUrl())
                .WithMessage("'{PropertyValue}' is not an absolute URL.");

            credentials.RuleFor(x => x.TokenUrl).Must(x => x.IsLocalUrl() is false).When(_ => isProduction)
                .WithMessage("'{PropertyValue}' is a local address. Configure the production token URL in appsettings.Production.json or environment variables.");

            credentials.RuleFor(x => x.ClientId).NotEmpty();
            credentials.RuleFor(x => x.ClientSecret).NotEmpty().WithMessage("The client secret is empty. Supply it through environment variables or a secret store.");
        });

        RuleFor(x => x.HealthCheck!).ChildRules(healthCheck =>
        {
            healthCheck.RuleFor(x => x.ReadinessEndpoint).NotEmpty();
            healthCheck.RuleFor(x => x.TimeoutSeconds).GreaterThan(0);
        });
    }
}
