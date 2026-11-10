using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aurore.Foundation.AspNetCore.MinimalApis.Abstractions;

namespace Aurore.Foundation.AspNetCore.MinimalApis;

/// <summary>
/// A resource group together with the endpoints that belong to it.
/// </summary>
public sealed record ResourceEndpoints(ResourceGroup Group, IReadOnlyList<IMinimalEndpoint> Endpoints);

/// <summary>
/// Everything discovered and validated at startup: the single source of truth for resources, endpoints and API versions.
/// Built once by AddAuroreMinimalEndpoints; consumed by endpoint mapping and OpenAPI/Scalar documents.
/// </summary>
public sealed class MinimalEndpointCatalog
{
    private MinimalEndpointCatalog(IReadOnlyList<ResourceEndpoints> resources)
    {
        Resources = resources;

        Versions =
        [
            .. resources
                .SelectMany(x => x.Group.Versions)
                .GroupBy(x => x.Number)
                .OrderBy(x => x.Key)
                .Select(Merge)
        ];
    }

    /// <summary>Every resource group with its endpoints.</summary>
    public IReadOnlyList<ResourceEndpoints> Resources { get; }

    /// <summary>
    /// Every version declared by any resource group, distinct and ascending. Groups declaring the same version must
    /// agree on its deprecation, sunset date and description (validated when the catalog is created).
    /// </summary>
    public IReadOnlyList<MinimalEndpointVersion> Versions { get; }

    internal static MinimalEndpointCatalog Create(Assembly assembly)
    {
        return Create(assembly.GetTypes());
    }

    internal static MinimalEndpointCatalog Create(IReadOnlyCollection<Type> types)
    {
        var groups = types
            .Where(x => x.IsAbstract is false && x.IsSubclassOf(typeof(ResourceGroup)))
            .Select(CreateInstance<ResourceGroup>)
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        ValidateUniqueNames(groups);

        var endpointTypes = types
            .Where(x => x is { IsClass: true, IsAbstract: false })
            .Select(x => (Type: x, Group: GetGroupType(x)))
            .Where(x => x.Group is not null)
            .ToList();

        var resources = new List<ResourceEndpoints>();

        foreach (var group in groups)
        {
            var endpoints = endpointTypes
                .Where(x => x.Group == group.GetType())
                .Select(x => CreateInstance<IMinimalEndpoint>(x.Type))
                .OrderBy(x => x.Version)
                .ThenBy(x => x.RouteName, StringComparer.Ordinal)
                .ToList();

            ValidateVersions(group, endpoints);

            resources.Add(new ResourceEndpoints(group, endpoints));
        }

        ValidateSharedVersions(resources);
        ValidateUniqueRouteNames(resources);

        return new MinimalEndpointCatalog(resources);
    }

    /// <summary>
    /// Route names are global to the app (they identify endpoints for link generation and in the API documents).
    /// </summary>
    private static void ValidateUniqueRouteNames(List<ResourceEndpoints> resources)
    {
        var endpoints = resources.SelectMany(x => x.Endpoints).ToList();

        if (endpoints.GroupBy(x => x.RouteName, StringComparer.Ordinal).FirstOrDefault(x => x.Count() > 1) is not { } duplicate)
            return;

        var types = string.Join(", ", duplicate.Select(x => x.GetType().Name));

        throw new InvalidOperationException($"Endpoints [{types}] share the route name '{duplicate.Key}'. Override Name on one of them to make it unique.");
    }

    private static MinimalEndpointVersion Merge(IGrouping<int, MinimalEndpointVersion> versions)
    {
        return new MinimalEndpointVersion(
            versions.Key,
            versions.First().IsDeprecated,
            versions.Select(x => x.Sunset).OfType<DateOnly>().Select(x => (DateOnly?)x).FirstOrDefault(),
            versions.Select(x => x.Description).OfType<string>().FirstOrDefault());
    }

    /// <summary>
    /// A version is up or down for the whole API: groups declaring the same version number must agree on whether it is
    /// deprecated, and on its sunset date and description when they give one.
    /// </summary>
    private static void ValidateSharedVersions(List<ResourceEndpoints> resources)
    {
        foreach (var versions in resources.SelectMany(x => x.Group.Versions).GroupBy(x => x.Number))
        {
            var deprecations = versions.Select(x => x.IsDeprecated).Distinct().Count();
            var sunsets = versions.Select(x => x.Sunset).OfType<DateOnly>().Distinct().ToArray();
            var descriptions = versions.Select(x => x.Description).OfType<string>().Distinct().ToArray();

            var conflict = (deprecations, sunsets.Length, descriptions.Length) switch
            {
                ( > 1, _, _) => "deprecation (deprecated in some groups, active in others; use a new version number instead)",
                (_, > 1, _) => $"sunset dates [{string.Join(", ", sunsets)}]",
                (_, _, > 1) => $"descriptions [{string.Join(" | ", descriptions)}]",
                _ => null,
            };

            if (conflict is not null)
            {
                throw new InvalidOperationException($"Version {versions.Key} is declared with conflicting {conflict} by different resource groups.");
            }
        }
    }

    private static void ValidateUniqueNames(List<ResourceGroup> groups)
    {
        if (groups.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1) is not { } duplicate)
            return;

        var types = string.Join(", ", duplicate.Select(x => x.GetType().Name));

        throw new InvalidOperationException($"Resource groups [{types}] share the name '{duplicate.Key}'. Group names must be unique.");
    }

    private static void ValidateVersions(ResourceGroup group, List<IMinimalEndpoint> endpoints)
    {
        var declared = group.Versions.Select(x => x.Number).ToArray();
        var declaredText = $"[{string.Join(", ", declared)}]";

        var error = declared switch
        {
            [] => "must declare at least one version",
            _ when declared.Min() <= 0 => $"declares invalid versions {declaredText}; versions must be greater than 0",
            _ when declared.Length != declared.Distinct().Count() => $"declares duplicate versions {declaredText}",
            _ when group.Versions.Where(x => x is { Sunset: not null, IsDeprecated: false }).Select(x => x.Number).ToArray() is { Length: > 0 } notDeprecated =>
                $"sets a sunset date on version(s) [{string.Join(", ", notDeprecated)}] that are not deprecated",
            _ when endpoints.FirstOrDefault(x => declared.Contains(x.Version) is false) is { } stray =>
                $"has endpoint '{stray.GetType().Name}' targeting version {stray.Version}, but only declares {declaredText}",
            _ when declared.Where(version => endpoints.All(x => x.Version != version)).ToArray() is { Length: > 0 } missing =>
                $"declares version(s) [{string.Join(", ", missing)}] without any endpoint",
            _ => null,
        };

        if (error is not null)
        {
            throw new InvalidOperationException($"Resource group '{group.GetType().Name}' {error}.");
        }
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
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException($"Failed to create '{type.FullName}'. Its constructor threw: {ex.InnerException.Message}", ex.InnerException);
        }
    }

    private static Type? GetGroupType(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current is { IsGenericType: true } generic && generic.GetGenericTypeDefinition() == typeof(MinimalEndpoint<>))
                return generic.GetGenericArguments()[0];
        }

        return null;
    }
}
