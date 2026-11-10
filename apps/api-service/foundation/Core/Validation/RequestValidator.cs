using FluentValidation;

namespace Aurore.Foundation.Core.Validation;

/// <summary>
/// Base class for validators of the shape of a request: derive from it and write the rules in the constructor. It is
/// registered as <see cref="IRequestValidator{T}"/> and run by the validation filter.
/// </summary>
/// <typeparam name="T">The request type being validated.</typeparam>
public abstract class RequestValidator<T> : AbstractValidator<T>, IRequestValidator<T>
{
}
