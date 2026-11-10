using FluentValidation;

namespace Aurore.Foundation.Core.Validation;

/// <summary>
/// Marks a validator of the shape of a request. The validation filter runs every one registered for a handler parameter
/// before the handler, and answers an invalid request with a 400. Business rules do not belong here: they use
/// <see cref="IFeatureValidator{T}"/>.
/// </summary>
/// <typeparam name="T">The request type being validated.</typeparam>
public interface IRequestValidator<T> : IValidator<T>
{
}
