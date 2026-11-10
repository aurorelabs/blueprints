using FluentValidation;

namespace Aurore.Foundation.Core.Validation;

/// <summary>
/// Marks a validator of business rules, run inside the feature pipeline and never by the HTTP validation filter. Keeping it
/// apart from <see cref="IRequestValidator{T}"/> means one validator is never run twice for the same request.
/// </summary>
/// <typeparam name="T">The type being validated.</typeparam>
public interface IFeatureValidator<T> : IValidator<T>
{
}
