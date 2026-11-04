using System;
using System.Globalization;
using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Foundation.Core.Extensions;
using FluentValidation;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter;

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
            });
        });

        RuleFor(x => x.IdentityProvider).NotNull().ChildRules(identityProvider =>
        {
            identityProvider.RuleFor(x => x.AuthorityUrl).Must(BeAnAbsoluteUrl).WithMessage("'{PropertyValue}' is not an absolute URL.");

            identityProvider.RuleFor(x => x.AuthorityUrl).Must(BeNonLocal).When(_ => isProduction)
                .WithMessage("'{PropertyValue}' is a local address. Configure the production authority in appsettings.Production.json or environment variables.");
            identityProvider.RuleFor(x => x.Audience).NotEmpty();
            identityProvider.RuleFor(x => x.AuthorizationPath).NotEmpty();
            identityProvider.RuleFor(x => x.TokenPath).NotEmpty();
        });

        RuleFor(x => x.Telemetry).NotNull().ChildRules(telemetry =>
        {
            telemetry.RuleFor(x => x.CollectorEndpoint).Must(BeAnAbsoluteUrl).WithMessage("'{PropertyValue}' is not an absolute URL.");

            telemetry.RuleFor(x => x.CollectorEndpoint).Must(BeNonLocal).When(_ => isProduction)
                .WithMessage("'{PropertyValue}' is a local address. Configure the production collector in appsettings.Production.json or environment variables.");
            telemetry.RuleFor(x => x.Protocol).IsEnumName(typeof(OtlpExportProtocol), caseSensitive: false);
            telemetry.RuleFor(x => x.Tracing.TraceIdRatioBasedSampler).InclusiveBetween(0, 1);
        });

        RuleFor(x => x.HealthChecks.CacheSeconds).GreaterThanOrEqualTo(0);

        RuleFor(x => x.ApiKeys.HeaderName).NotEmpty();

        RuleForEach(x => x.ApiKeys.Clients).ChildRules(client =>
        {
            client.RuleFor(x => x.Value.Key).NotEmpty().WithMessage("API key client '{PropertyName}' has an empty Key.");

            client.RuleFor(x => x.Value.Key).MinimumLength(32).When(_ => isProduction)
                .WithMessage("Production API keys must be at least 32 characters. Supply them through environment variables or a secret store.");
        });

        RuleForEach(x => x.Dependencies).ChildRules(dependency =>
        {
            dependency.RuleFor(x => x.Value.BaseUrl).Must(BeAnAbsoluteUrl).WithMessage("'{PropertyValue}' is not an absolute URL.");
        });
    }

    private static bool BeAnAbsoluteUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out _);
    }

    private static bool BeNonLocal(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) is false
            || (uri.IsLoopback is false && uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) is false);
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
