namespace Aurore.Foundation.Core.Features;

/// <summary>
/// Marks a feature (a use case) so the scan finds it. Features derive from <see cref="Feature{TRequest}"/> or
/// <see cref="Feature{TRequest, TResponse}"/>, which implement it.
/// </summary>
public interface IFeature
{
}
