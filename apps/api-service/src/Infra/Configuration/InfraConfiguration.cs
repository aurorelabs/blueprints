using System;
using Aurore.Blueprints.ApiService.Core.Clients.GitHub;
using Aurore.Blueprints.ApiService.Core.Constants;
using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Blueprints.ApiService.Infra.Clients.GitHub;
using Aurore.Blueprints.ApiService.Infra.Clients.RandomUser;
using Aurore.Foundation.AspNetCore.HttpDependencies.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aurore.Blueprints.ApiService.Infra.Configuration;

public static class InfraConfiguration
{
    public static IServiceCollection AddAppInfra(this IServiceCollection services, AppSettings appSettings)
    {
        return services.AddAppHttpDependencies(appSettings);
    }

    private static IServiceCollection AddAppHttpDependencies(this IServiceCollection services, AppSettings appSettings)
    {
        var dependencies = appSettings.HttpDependencies;

        // Typed, anonymous.
        services.AddAuroreHttpDependency<IGitHubClient, GitHubClient>(HttpDependencyNames.GitHub, dependencies.GetRequired(HttpDependencyNames.GitHub));

        // Untyped, custom authentication handler.
        services.TryAddSingleton(TimeProvider.System);
        services.AddTransient<RandomUserSigningHandler>();
        services
            .AddAuroreHttpDependency(HttpDependencyNames.RandomUser, dependencies.GetRequired(HttpDependencyNames.RandomUser))
            .AddHttpMessageHandler<RandomUserSigningHandler>();

        // Untyped, OAuth2 client credentials.
        var orders = dependencies.GetRequired(HttpDependencyNames.Orders);
        services
            .AddAuroreHttpDependency(HttpDependencyNames.Orders, orders)
            .AddAuroreClientCredentials(orders.ClientCredentials);

        // Untyped, static API key header.
        var inventory = dependencies.GetRequired(HttpDependencyNames.Inventory);
        services
            .AddAuroreHttpDependency(HttpDependencyNames.Inventory, inventory)
            .AddAuroreApiKey(inventory.ApiKey);

        return services;
    }
}
