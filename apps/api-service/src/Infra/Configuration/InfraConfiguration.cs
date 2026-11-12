using Aurore.Blueprints.ApiService.Core.Clients.GitHub;
using Aurore.Blueprints.ApiService.Core.Constants;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Blueprints.ApiService.Infra.Clients.GitHub;
using Aurore.Blueprints.ApiService.Infra.Clients.RandomUser;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Blueprints.ApiService.Infra.Contexts.Configurations;
using Aurore.Foundation.AspNetCore.HttpDependencies.Configuration;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Configuration;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Features;
using Aurore.Foundation.EntityFrameworkCore.Postgres.HealthChecks;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Blueprints.ApiService.Infra.Configuration;

public static class InfraConfiguration
{
    public static IServiceCollection AddAppInfra(this IServiceCollection services, AppSettings appSettings)
    {
        services.AddAppDatabase(appSettings.Postgres);
        services.AddPostgresHealthCheck(appSettings.Postgres);

        return services.AddAppHttpDependencies(appSettings);
    }

    /// <summary>
    /// The database context with the conventions of the foundation, and the errors that unique constraints answer with when two
    /// requests create the same value at once (checked by the database, so there is no check-then-insert race).
    /// </summary>
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, PostgresOptions postgres)
    {
        return services
            .AddAuroreDbContext<AppDbContext>(postgres)
            .AddUniqueViolationErrors(map => map
                .On(AuthorConfiguration.UniqueHandleIndex, AppErrors.Authors.HandleTaken)
                .On(PostConfiguration.UniqueSlugIndex, AppErrors.Posts.SlugTaken));
    }

    private static IServiceCollection AddAppHttpDependencies(this IServiceCollection services, AppSettings appSettings)
    {
        var dependencies = appSettings.HttpDependencies;

        // Typed, anonymous.
        services.AddAuroreHttpDependency<IGitHubClient, GitHubClient>(HttpDependencyNames.GitHub, dependencies.GetRequired(HttpDependencyNames.GitHub));

        // Untyped, custom authentication handler.
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
