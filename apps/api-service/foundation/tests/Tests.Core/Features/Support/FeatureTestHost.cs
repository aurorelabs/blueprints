using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Features.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tests.Core.Features.Support;

/// <summary>Builds a container with the sample features, so each test can enable the decorators it needs.</summary>
public static class FeatureTestHost
{
    public static ServiceProvider Provider(Action<FeaturesBuilder>? configure = null, Action<IServiceCollection>? register = null)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<Trace>()
            .AddFeaturesFrom<Ping>(configure);

        register?.Invoke(services);

        return services.BuildServiceProvider();
    }

    public static FeatureContext ContextOf<TFeature>(Ping request, IServiceProvider? services = null)
    {
        return new FeatureContext
        {
            FeatureType = typeof(TFeature),
            RequestType = typeof(Ping),
            Request = request,
            Services = services ?? new ServiceCollection().BuildServiceProvider(),
        };
    }

    public static ILogger<T> NullLogger<T>()
    {
        return Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
    }
}
