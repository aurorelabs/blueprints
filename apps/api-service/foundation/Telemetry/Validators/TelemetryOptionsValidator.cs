using Aurore.Foundation.Telemetry.Models;
using FluentValidation;

namespace Aurore.Foundation.Telemetry.Validators;

/// <summary>Checks the telemetry settings at startup.</summary>
public sealed class TelemetryOptionsValidator : AbstractValidator<TelemetryOptions>
{
    /// <summary>Creates the validator. The flag turns on the stricter production rules.</summary>
    public TelemetryOptionsValidator(bool isProduction)
    {
        RuleFor(x => x.Otlp).SetValidator(new TelemetryOtlpOptionsValidator(isProduction));

        RuleFor(x => x.Logging.BatchExport).SetValidator(new TelemetryBatchExportOptionsValidator());

        RuleFor(x => x.Tracing.BatchExport).SetValidator(new TelemetryBatchExportOptionsValidator());
        RuleFor(x => x.Tracing.SamplingRatio).InclusiveBetween(0, 1);

        RuleFor(x => x.Metrics.ExportIntervalSeconds).GreaterThan(0);
        RuleFor(x => x.Metrics.ExportTimeoutSeconds).GreaterThan(0);
    }
}
