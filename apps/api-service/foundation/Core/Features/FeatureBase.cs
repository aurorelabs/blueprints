namespace Aurore.Foundation.Core.Features;

/// <summary>The part of every feature that carries its pipeline. Derive from the generic Feature classes instead.</summary>
public abstract class FeatureBase : IFeature
{
    internal FeatureBase()
    {
    }

    /// <summary>Set by the registration; null when the feature is built by hand, for example in a test.</summary>
    internal FeaturePipeline? Pipeline { get; set; }

    internal void Attach(FeaturePipeline pipeline)
    {
        Pipeline = pipeline;
    }
}
