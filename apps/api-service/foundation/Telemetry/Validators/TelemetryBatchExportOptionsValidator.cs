using Aurore.Foundation.Telemetry.Models;
using FluentValidation;

namespace Aurore.Foundation.Telemetry.Validators;

/// <summary>Checks the batch export settings.</summary>
public sealed class TelemetryBatchExportOptionsValidator : AbstractValidator<TelemetryBatchExportOptions>
{
    /// <summary>Creates the validator.</summary>
    public TelemetryBatchExportOptionsValidator()
    {
        RuleFor(x => x.MaxQueueSize).GreaterThan(0);
        RuleFor(x => x.MaxExportBatchSize).GreaterThan(0).LessThanOrEqualTo(x => x.MaxQueueSize);
        RuleFor(x => x.ScheduledDelaySeconds).GreaterThan(0);
        RuleFor(x => x.ExportTimeoutSeconds).GreaterThan(0);
    }
}
