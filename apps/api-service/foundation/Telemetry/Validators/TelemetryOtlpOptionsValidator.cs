using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Telemetry.Models;
using FluentValidation;
using OpenTelemetry.Exporter;

namespace Aurore.Foundation.Telemetry.Validators;

public sealed class TelemetryOtlpOptionsValidator : AbstractValidator<TelemetryOtlpOptions>
{
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
