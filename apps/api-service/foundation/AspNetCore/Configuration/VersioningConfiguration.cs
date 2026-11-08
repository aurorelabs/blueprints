using System;
using Asp.Versioning;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.AspNetCore.Configuration;

public static class VersioningConfiguration
{
    /// <summary>
    /// How an API version is named by the API explorer and by the OpenAPI documents (v1, v2...). Single source for both.
    /// </summary>
    public const string GroupNameFormat = "'v'VVV";

    /// <summary>
    /// Versions are declared once, by the resource groups; sunset dates come from the same catalog.
    /// </summary>
    public static IServiceCollection AddAuroreApiVersioning(this IServiceCollection services, MinimalEndpointCatalog endpoints)
    {
        services
            .AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.DefaultApiVersion = new ApiVersion(1);
                options.ApiVersionReader = new UrlSegmentApiVersionReader();

                foreach (var version in endpoints.Versions)
                {
                    if (version.Sunset is not { } sunset)
                        continue;

                    options.Policies
                        .Sunset(new ApiVersion(version.Number))
                        .Effective(new DateTimeOffset(sunset.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
                }
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = GroupNameFormat;
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
