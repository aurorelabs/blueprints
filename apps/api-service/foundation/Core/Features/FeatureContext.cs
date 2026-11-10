using System;
using System.Threading;

namespace Aurore.Foundation.Core.Features;

/// <summary>What a decorator knows about the call it wraps.</summary>
public sealed class FeatureContext
{
    /// <summary>The feature being run, for example SearchCustomers.</summary>
    public required Type FeatureType { get; init; }

    /// <summary>The declared request type of the feature (not necessarily the type of the object passed).</summary>
    public required Type RequestType { get; init; }

    /// <summary>The request passed to the feature.</summary>
    public required object Request { get; init; }

    /// <summary>The services of the scope the feature runs in.</summary>
    public required IServiceProvider Services { get; init; }

    /// <summary>The cancellation token of the call.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>The name of the feature, used by telemetry and logs.</summary>
    public string FeatureName => FeatureType.Name;
}
