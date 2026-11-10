using System;
using Aurore.Foundation.Core.Features.Decorators;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>Enables a decorator for this feature: [With&lt;MyDecorator&gt;].</summary>
/// <typeparam name="TDecorator">The decorator to enable.</typeparam>
public sealed class WithAttribute<TDecorator> : FeatureDecoratorAttribute
    where TDecorator : class, IFeatureDecorator
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(TDecorator);
}

/// <summary>Disables a decorator for this feature, even when it is enabled for all: [Without&lt;LoggingDecorator&gt;].</summary>
/// <typeparam name="TDecorator">The decorator to disable.</typeparam>
public sealed class WithoutAttribute<TDecorator> : FeatureDecoratorAttribute
    where TDecorator : class, IFeatureDecorator
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(TDecorator);

    /// <inheritdoc />
    public override bool Removes => true;
}

/// <summary>Enables the telemetry span for this feature.</summary>
public sealed class WithTelemetryAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(TelemetryDecorator);
}

/// <summary>Enables the log line (outcome and duration) for this feature.</summary>
public sealed class WithLoggingAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(LoggingDecorator);
}

/// <summary>Enables the business validation (feature validators) for this feature.</summary>
public sealed class WithValidationAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(ValidationDecorator);
}
