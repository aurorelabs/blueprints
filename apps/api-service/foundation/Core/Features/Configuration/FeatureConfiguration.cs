using System;
using System.Collections.Generic;
using System.Linq;
using Aurore.Foundation.Core.Features.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aurore.Foundation.Core.Features.Configuration;

/// <summary>Registers the features of an assembly.</summary>
public static class FeatureConfiguration
{
    /// <summary>
    /// Scans the assembly of TAssembly for features and registers each one as itself, so it is injected by its own type. The
    /// decorators of a feature are those enabled in the builder, plus the With attributes of the feature, minus its Without
    /// attributes. Nothing is enabled unless asked for.
    /// </summary>
    public static IServiceCollection AddFeaturesFrom<TAssembly>(
        this IServiceCollection services,
        Action<FeaturesBuilder>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        var builder = new FeaturesBuilder();
        configure?.Invoke(builder);

        var features = typeof(TAssembly).Assembly.GetTypes()
            .Where(x => x is { IsClass: true, IsAbstract: false } && typeof(FeatureBase).IsAssignableFrom(x));

        foreach (var feature in features)
        {
            var decorators = DecoratorsOf(feature, builder.Decorators);

            foreach (var decorator in decorators)
                services.TryAddTransient(decorator);

            services.Add(new ServiceDescriptor(feature, provider => Create(provider, feature, decorators), lifetime));
        }

        return services;
    }

    private static Type[] DecoratorsOf(Type feature, IReadOnlyList<Type> enabledForAll)
    {
        var decorators = new List<Type>(enabledForAll);
        var attributes = feature.GetCustomAttributes(typeof(FeatureDecoratorAttribute), inherit: false).Cast<FeatureDecoratorAttribute>().ToArray();

        foreach (var attribute in attributes.Where(x => x.Removes is false))
        {
            if (decorators.Contains(attribute.DecoratorType) is false)
                decorators.Add(attribute.DecoratorType);
        }

        // Without wins over With, so a feature can switch off what a registration enabled for all.
        foreach (var attribute in attributes.Where(x => x.Removes))
            decorators.Remove(attribute.DecoratorType);

        return [.. decorators];
    }

    private static object Create(IServiceProvider provider, Type feature, Type[] decorators)
    {
        var instance = (FeatureBase)ActivatorUtilities.CreateInstance(provider, feature);

        if (decorators.Length > 0)
            instance.Attach(new FeaturePipeline(provider, decorators));

        return instance;
    }
}
