using System;
using System.Collections.Generic;
using Aurore.Foundation.Core.Features.Decorators;

namespace Aurore.Foundation.Core.Features.Configuration;

/// <summary>
/// Chooses the decorators enabled for every feature of a registration. Nothing is on unless asked for, so a host such as a
/// background worker can enable a different set for the same features. One feature can still add or remove a decorator with
/// the With and Without attributes.
/// </summary>
public sealed class FeaturesBuilder
{
    private readonly List<Type> _decorators = [];

    internal IReadOnlyList<Type> Decorators => _decorators;

    /// <summary>Runs the business validators (IFeatureValidator) of the request before the feature.</summary>
    public FeaturesBuilder WithValidation()
    {
        return With<ValidationDecorator>();
    }

    /// <summary>Runs every feature inside a span.</summary>
    public FeaturesBuilder WithTelemetry()
    {
        return With<TelemetryDecorator>();
    }

    /// <summary>Writes one log line per run with the outcome and the time it took.</summary>
    public FeaturesBuilder WithLogging()
    {
        return With<LoggingDecorator>();
    }

    /// <summary>Enables a decorator of your own for every feature.</summary>
    public FeaturesBuilder With<TDecorator>()
        where TDecorator : class, IFeatureDecorator
    {
        if (_decorators.Contains(typeof(TDecorator)) is false)
            _decorators.Add(typeof(TDecorator));

        return this;
    }
}
