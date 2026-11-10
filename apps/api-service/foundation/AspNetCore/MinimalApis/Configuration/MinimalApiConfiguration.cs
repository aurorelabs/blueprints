using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Aurore.Foundation.AspNetCore.MinimalApis.Abstractions;
using Aurore.Foundation.AspNetCore.RequestPipeline.Filters;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Aurore.Foundation.AspNetCore.MinimalApis.Configuration;

/// <summary>Discovers the resource groups and endpoints of the application and maps them.</summary>
public static class MinimalApiConfiguration
{
    /// <summary>
    /// Default problem responses every endpoint can return, documented once for the whole API. 400 is documented
    /// separately as a validation problem (with the "errors" dictionary). Pass your own list to
    /// <see cref="MapAuroreMinimalEndpoints"/> to replace these.
    /// </summary>
    public static readonly IReadOnlyList<int> StandardProblems =
    [
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status429TooManyRequests,
        StatusCodes.Status500InternalServerError,
    ];

    /// <summary>
    /// Discovers and validates every resource group and endpoint in the assembly of <typeparamref name="TAssembly"/>.
    /// The returned catalog is the single source of truth for mapping and API documents. Throws when the configuration
    /// is inconsistent (endpoint targeting an undeclared version, version without endpoints, duplicate group names...).
    /// </summary>
    public static IHostApplicationBuilder AddAuroreMinimalEndpoints<TAssembly>(this IHostApplicationBuilder builder, out MinimalEndpointCatalog catalog)
    {
        catalog = MinimalEndpointCatalog.Create(typeof(TAssembly).Assembly);

        return builder;
    }

    /// <summary>
    /// Maps the endpoints of the catalog built by <see cref="AddAuroreMinimalEndpoints{TAssembly}"/>.
    /// </summary>
    /// <param name="builder">The route builder (usually the WebApplication).</param>
    /// <param name="catalog">The catalog returned by AddAuroreMinimalEndpoints.</param>
    /// <param name="configure">Conventions applied to every endpoint.</param>
    /// <param name="globalProblems">
    /// Problem status codes (400-599) documented on every endpoint. Replaces <see cref="StandardProblems"/> when given;
    /// null keeps the defaults.
    /// </param>
    public static IEndpointRouteBuilder MapAuroreMinimalEndpoints(
        this IEndpointRouteBuilder builder, MinimalEndpointCatalog catalog, Action<RouteGroupBuilder>? configure = null, IEnumerable<int>? globalProblems = null)
    {
        var problems = ResolveGlobalProblems(globalProblems);

        var api = builder
            .MapGroup(string.Empty)
            .ProducesValidationProblem()
            .AddEndpointFilterFactory(RequestValidationFilter.Create);

        foreach (var problem in problems)
            api.ProducesProblem(problem);

        configure?.Invoke(api);

        foreach (var (group, endpoints) in catalog.Resources)
        {
            var prefix = group.PrefixWithApi ? "api/v{version:apiVersion}" : "v{version:apiVersion}";

            var groupBuilder = api
                .MapGroup($"{prefix}/{group.Name}")
                .WithTags([.. group.Tags.Where(x => x.HasValue())])
                .ConfigureProblems(group, problems)
                .ConfigureVersionSet(group)
                .ConfigureAuthorization(group);

            group.Configure(groupBuilder);

            groupBuilder.MapEndpoints(endpoints);
        }

        return builder;
    }

    internal static IReadOnlyList<int> ResolveGlobalProblems(IEnumerable<int>? globalProblems)
    {
        if (globalProblems?.Distinct().ToArray() is not { } custom)
            return StandardProblems;

        if (custom.Where(x => x is < 400 or > 599).ToArray() is { Length: > 0 } invalid)
        {
            throw new ArgumentOutOfRangeException(nameof(globalProblems), $"Problem status codes must be between 400 and 599 (got [{string.Join(", ", invalid)}]).");
        }

        return custom;
    }

    private static RouteGroupBuilder ConfigureProblems(this RouteGroupBuilder builder, ResourceGroup group, IReadOnlyList<int> globalProblems)
    {
        foreach (var problem in group.Problems.Except(globalProblems))
            builder.ProducesProblem(problem);

        return builder;
    }

    private static RouteGroupBuilder ConfigureVersionSet(this RouteGroupBuilder builder, ResourceGroup group)
    {
        var set = builder.NewApiVersionSet();

        foreach (var version in group.Versions)
        {
            var apiVersion = new ApiVersion(version.Number);

            _ = version switch
            {
                { IsDeprecated: true } => set.HasDeprecatedApiVersion(apiVersion),
                _ => set.HasApiVersion(apiVersion),
            };
        }

        builder.WithApiVersionSet(set.Build());

        return builder;
    }

    private static RouteGroupBuilder ConfigureAuthorization(this RouteGroupBuilder builder, ResourceGroup group)
    {
        return group.RequireAuthorization ? builder.RequireAuthorization() : builder;
    }

    private static void MapEndpoints(this RouteGroupBuilder builder, IReadOnlyList<IMinimalEndpoint> endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            var route = endpoint
                .Map(builder)
                .WithName(endpoint.RouteName)
                .WithSummary(endpoint.Summary)
                .MapToApiVersion(endpoint.Version);

            if (endpoint.Description is { } description)
                route.WithDescription(description);
        }
    }
}
