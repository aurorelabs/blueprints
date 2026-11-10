using Aurore.Foundation.Core.Errors;
using FluentValidation;

namespace Aurore.Foundation.Core.Validation;

/// <summary>
/// Base class for validators of business rules: derive from it and write the rules in the constructor. It is registered as
/// <see cref="IFeatureValidator{T}"/> and run inside the feature pipeline, never by the validation filter.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public abstract class FeatureValidator<T> : AbstractValidator<T>, IFeatureValidator<T>
{
    /// <summary>
    /// The error answered when a rule fails, such as a Conflict (409) for a duplicate; the failed rules become its details.
    /// Without one, the failure is a plain validation error (400).
    /// </summary>
    public virtual ApplicationError? Error => null;
}
