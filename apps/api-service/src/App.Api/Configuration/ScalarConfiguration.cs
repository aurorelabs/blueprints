using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.OpenApi.Extensions;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Aurore.Blueprints.ApiService.App.Api.Configuration;

internal static class ScalarConfiguration
{
    internal static IHostApplicationBuilder AddAppScalar(this IHostApplicationBuilder builder, AppSettings appSettings, MinimalEndpointCatalog endpoints)
    {
        if (builder.Environment.IsProduction())
            return builder;

        builder.ConfigureAuroreScalarDocuments(endpoints.Versions);

        foreach (var version in endpoints.Versions)
        {
            builder.Services
                .AddOpenApi(ScalarExtensions.DocumentName(version.Number), options => options
                    .AddAuroreDocumentInfo(version, appSettings.AppInformation)
                    .AddAuroreJwtBearerSecurity()
                    .AddAuroreApiKeySecurity(appSettings.ApiKeys)
                    .AddAuroreOidcSecurity(appSettings.IdentityProvider)
                    .AddAuroreOAuth2Security(appSettings.IdentityProvider)
                    .AddAuroreCorrelationIdHeader());
        }

        return builder;
    }

    internal static WebApplication UseAppScalar(this WebApplication app, AppSettings appSettings)
    {
        if (app.Environment.IsProduction())
            return app;

        app.MapOpenApi();

        app.MapScalarApiReference(options => options
            .WithTitle($"{appSettings.AppInformation.Component.ToTitleCase()} API")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl)
            .WithAuroreAuthentication(appSettings.IdentityProvider, appSettings.ApiDocs));

        return app;
    }
}
