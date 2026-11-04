using Aurore.Blueprints.ApiService.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Blueprints.ApiService.Features.Configuration;

public static class FeatureConfiguration
{
    public static IServiceCollection AddAppFeatures(this IServiceCollection services, AppSettings _)
    {
        return services;
    }
}
