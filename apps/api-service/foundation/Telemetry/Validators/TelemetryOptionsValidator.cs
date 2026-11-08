using Aurore.Foundation.Telemetry.Models;
using FluentValidation;

namespace Aurore.Foundation.Telemetry.Validators;

public sealed class TelemetryOptionsValidator : AbstractValidator<TelemetryOptions>
{
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
