using System.Threading.Tasks;
using Aurore.Foundation.Core.Results;

namespace Aurore.Foundation.Core.Features;

/// <summary>Runs the rest of the pipeline: the decorators after this one, and then the feature.</summary>
public delegate ValueTask<Result> FeatureDelegate();

/// <summary>
/// Wraps the run of a feature: do something before and after calling next, or answer with a failed result without calling it
/// at all. Register one for all features with FeaturesBuilder.With, or for one feature with the With attribute. Resolved from
/// the container, so it may take services in its constructor.
/// </summary>
public interface IFeatureDecorator
{
    /// <summary>
    /// Where the decorator sits in the pipeline: lower runs first (outermost). Telemetry is 100, logging 200 and validation
    /// 300; custom decorators default to 1000, which puts them right before the feature.
    /// </summary>
    public int Order => 1000;

    /// <summary>
    /// Runs the decorator. A failed result returned without calling next is turned into the failure of the feature's own
    /// result type.
    /// </summary>
    public ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next);
}
