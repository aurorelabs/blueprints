using System.Globalization;
using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Foundation.Core.Authentication.Validators;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.HttpDependencies.Validators;
using Aurore.Foundation.Telemetry.Validators;
using FluentValidation;
using Microsoft.Extensions.Hosting;

namespace Aurore.Blueprints.ApiService.Core.Validators;

public sealed class AppSettingsValidator : AbstractValidator<AppSettings>
{
    // IHostEnvironment instead of bool: the validator scan in AddAppValidators must be able to resolve it.
    public AppSettingsValidator(IHostEnvironment environment)
    {
        var isProduction = environment.IsProduction();

        RuleFor(x => x.AppInformation).NotNull().ChildRules(app =>
        {
            app.RuleFor(x => x.Domain).NotEmpty();
            app.RuleFor(x => x.Boundary).NotEmpty();
            app.RuleFor(x => x.Component).NotEmpty();
            app.RuleFor(x => x.Description).NotEmpty();
            app.RuleFor(x => x.Locale).Must(BeACulture).WithMessage("'{PropertyValue}' is not a valid culture name.");
            app.RuleFor(x => x.Maintainer).NotNull().ChildRules(maintainer =>
            {
                maintainer.RuleFor(x => x.Name).NotEmpty();
                maintainer.RuleFor(x => x.Email).NotEmpty().EmailAddress();
                maintainer.RuleFor(x => x.Url).Must(x => x.IsAbsoluteUrl()).When(x => x.Url.HasValue())
                    .WithMessage("'{PropertyValue}' is not an absolute URL.");
            });
        });

        RuleFor(x => x.IdentityProvider).NotNull().SetValidator(new IdentityProviderOptionsValidator(isProduction));
        RuleFor(x => x.Telemetry).NotNull().SetValidator(new TelemetryOptionsValidator(isProduction));

        RuleFor(x => x.HealthChecks.CacheSeconds).GreaterThanOrEqualTo(0);

        RuleFor(x => x.ApiKeys.HeaderName).NotEmpty();

        RuleForEach(x => x.ApiKeys.Clients).ChildRules(client =>
        {
            client.RuleFor(x => x.Value.Key).NotEmpty().WithMessage("API key client '{PropertyName}' has an empty Key.");

            client.RuleFor(x => x.Value.Key).MinimumLength(32).When(_ => isProduction)
                .WithMessage("Production API keys must be at least 32 characters. Supply them through environment variables or a secret store.");
        });

        // Reported with the configuration key (HttpDependencies:Orders:BaseUrl), not a positional index.
        RuleFor(x => x.HttpDependencies).Custom((dependencies, context) =>
        {
            var validator = new HttpDependencyOptionsValidator(isProduction);

            foreach (var (name, dependency) in dependencies)
            {
                foreach (var failure in validator.Validate(dependency).Errors)
                    context.AddFailure($"HttpDependencies:{name}:{failure.PropertyName}", failure.ErrorMessage);
            }
        });
    }

    private static bool BeACulture(string? value)
    {
        if (value.HasValue() is false)
            return false;

        try
        {
            return CultureInfo.GetCultureInfo(value, predefinedOnly: true) is not null;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
