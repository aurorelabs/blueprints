using System;

namespace Aurore.Foundation.Core.Features.Attributes;

/// <summary>
/// Base of the attributes that change which decorators run for one feature, on top of what AddFeaturesFrom enabled for all.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public abstract class FeatureDecoratorAttribute : Attribute
{
    /// <summary>The decorator the attribute adds or removes.</summary>
    public abstract Type DecoratorType { get; }

    /// <summary>True when the attribute removes the decorator instead of adding it.</summary>
    public virtual bool Removes => false;
}
