using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Foundation.Core.Features.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Blueprints.ApiService.Features.Configuration;

public static class FeatureConfiguration
{
    public static IServiceCollection AddAppFeatures(this IServiceCollection services, AppSettings _)
    {
        // Every decorator is opt-in: enable here what all features get, and add or remove one for a feature with the
        // With and Without attributes. A worker or message host can register the same features with another set.
        return services.AddFeaturesFrom<IFeatures>(features => features
            .WithTelemetry()
            .WithLogging()
            .WithValidation());
    }
}
