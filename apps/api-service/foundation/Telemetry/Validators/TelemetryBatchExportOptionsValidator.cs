using Aurore.Foundation.Telemetry.Models;
using FluentValidation;

namespace Aurore.Foundation.Telemetry.Validators;

public sealed class TelemetryBatchExportOptionsValidator : AbstractValidator<TelemetryBatchExportOptions>
{
    public TelemetryBatchExportOptionsValidator()
    {
        RuleFor(x => x.MaxQueueSize).GreaterThan(0);
        RuleFor(x => x.MaxExportBatchSize).GreaterThan(0).LessThanOrEqualTo(x => x.MaxQueueSize);
        RuleFor(x => x.ScheduledDelaySeconds).GreaterThan(0);
        RuleFor(x => x.ExportTimeoutSeconds).GreaterThan(0);
    }
}
