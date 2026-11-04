using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Foundation.AspNetCore.OpenApi.Extensions;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Aurore.Blueprints.ApiService.App.Api.Configuration;

internal static class ScalarConfiguration
{
    private static readonly int[] _availableVersions = [1, 2];

    internal static IHostApplicationBuilder AddAppScalar(this IHostApplicationBuilder builder, AppSettings appSettings)
    {
        if (builder.Environment.IsProduction())
            return builder;

        var availableVersions = builder.ConfigureAuroreScalarDocuments(_availableVersions);

        foreach (var version in availableVersions)
        {
            builder.Services
                .AddOpenApi(version, options => options
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
            .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl));

        return app;
    }
}
