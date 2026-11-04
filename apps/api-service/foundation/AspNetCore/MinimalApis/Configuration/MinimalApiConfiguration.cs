using System;
using System.Linq;
using Asp.Versioning;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Aurore.Foundation.AspNetCore.MinimalApis.Configuration;

public static class MinimalApiConfiguration
{
    /// <summary>
    /// Problem responses every endpoint can return, documented once for the whole API. 400 is documented separately as
    /// a validation problem (with the "errors" dictionary).
    /// </summary>
    public static readonly int[] StandardProblems =
    [
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status429TooManyRequests,
        StatusCodes.Status500InternalServerError,
    ];

    public static IEndpointRouteBuilder MapAuroreResourcesFromAssembly<TAssembly>(this IEndpointRouteBuilder builder, Action<RouteGroupBuilder>? configure = null)
    {
        var environment = builder.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var types = typeof(TAssembly).Assembly.GetTypes();

        var api = builder.MapGroup(string.Empty).ProducesValidationProblem();

        foreach (var problem in StandardProblems)
            api.ProducesProblem(problem);

        configure?.Invoke(api);

        var groups = types
            .Where(x => x.IsAbstract is false && x.IsSubclassOf(typeof(ResourceGroup)))
            .Select(CreateInstance<ResourceGroup>)
            .Where(x => x.IsAvailable(environment));

        foreach (var group in groups)
        {
            var prefix = group.PrefixWithApi ? "api/v{version:apiVersion}" : "v{version:apiVersion}";

            var groupBuilder = api
                .MapGroup($"{prefix}/{group.Name}")
                .WithTags([.. group.Tags.Where(x => x.HasValue())])
                .ConfigureProblems(group)
                .ConfigureVersionSet(group)
                .ConfigureAuthorization(group);

            group.Configure(groupBuilder);

            groupBuilder.MapEndpoints(types, group);
        }

        return builder;
    }

    private static T CreateInstance<T>(Type type)
    {
        try
        {
            return (T)Activator.CreateInstance(type)!;
        }
        catch (MissingMethodException ex)
        {
            throw new InvalidOperationException($"Failed to create '{type.FullName}'. It needs a public parameterless constructor.", ex);
        }
    }

    private static RouteGroupBuilder ConfigureProblems(this RouteGroupBuilder builder, ResourceGroup group)
    {
        foreach (var problem in group.Problems.Except(StandardProblems))
            builder.ProducesProblem(problem);

        return builder;
    }

    private static RouteGroupBuilder ConfigureVersionSet(this RouteGroupBuilder builder, ResourceGroup group)
    {
        var set = builder.NewApiVersionSet().ReportApiVersions();

        foreach (var version in group.Versions)
            set.HasApiVersion(new ApiVersion(version));

        builder.WithApiVersionSet(set.Build());

        return builder;
    }

    private static RouteGroupBuilder ConfigureAuthorization(this RouteGroupBuilder builder, ResourceGroup group)
    {
        return group.RequireAuthorization ? builder.RequireAuthorization() : builder;
    }

    private static void MapEndpoints(this RouteGroupBuilder builder, Type[] types, ResourceGroup group)
    {
        var groupType = group.GetType();

        var endpoints = types
            .Where(x => x is { IsClass: true, IsAbstract: false } && GetGroupType(x) == groupType)
            .Select(CreateInstance<MinimalEndpoint>);

        foreach (var endpoint in endpoints)
        {
            var route = endpoint
                .Map(builder)
                .WithName(endpoint.RouteName)
                .WithSummary(endpoint.Summary)
                .MapToApiVersion(endpoint.Version);

            if (endpoint.Description is not null)
                route.WithDescription(endpoint.Description);
        }
    }

    private static Type? GetGroupType(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current is { IsGenericType: true } && current.GetGenericTypeDefinition() == typeof(MinimalEndpoint<>))
                return current.GetGenericArguments()[0];
        }

        return null;
    }
}
