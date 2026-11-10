using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Telemetry.Models;
using FluentValidation;
using OpenTelemetry.Exporter;

namespace Aurore.Foundation.Telemetry.Validators;

/// <summary>Checks the collector connection settings.</summary>
public sealed class TelemetryOtlpOptionsValidator : AbstractValidator<TelemetryOtlpOptions>
{
    /// <summary>Creates the validator. The flag turns on the stricter production rules.</summary>
    public TelemetryOtlpOptionsValidator(bool isProduction)
    {
        RuleFor(x => x.Endpoint).Must(x => x.IsAbsoluteUrl())
            .WithMessage("'{PropertyValue}' is not an absolute URL.");

        RuleFor(x => x.Endpoint).Must(x => x.IsLocalUrl() is false).When(_ => isProduction)
            .WithMessage("'{PropertyValue}' is a local address. Configure the production collector in appsettings.Production.json or environment variables.");

        RuleFor(x => x.Protocol).IsEnumName(typeof(OtlpExportProtocol), caseSensitive: false);
        RuleFor(x => x.RequestTimeoutSeconds).GreaterThan(0);
    }
}
