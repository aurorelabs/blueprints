using System;
using Aurore.Blueprints.ApiService.Core;
using Aurore.Foundation.AspNetCore;
using Aurore.Foundation.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Aurore.Blueprints.ApiService.App.Api.Files;

internal static class AppSettingsConfiguration
{
    internal static IHostApplicationBuilder AddAppSettings(this IHostApplicationBuilder builder, out AppSettings appSettings)
    {
        appSettings = builder.Configuration.Get<AppSettings>() ?? throw new InvalidOperationException("Failed to load appsettings.json correctly.");

        builder.Services.AddSingleton(appSettings);

        return builder;
    }
}

internal static class ScalarConfiguration
{
    private static readonly int[] _availableVersions = [1, 2];

    internal static IHostApplicationBuilder AddScalar(this IHostApplicationBuilder builder, AppSettings appSettings)
    {
        if (builder.Environment.IsProduction())
            return builder;

        var availableVersions = builder.ConfigureScalarDocuments(_availableVersions);

        foreach (var version in availableVersions)
        {
            builder.Services
                .AddOpenApi(version, options => options
                    .AddAppInfo(version, appSettings.AppInformation)
                    .AddJwtBearerSecurity()
                    .AddOidcSecurity(appSettings.IdentityProvider)
                    .AddOAuth2Security(appSettings.IdentityProvider)
                    .AddCorrelationIdHeader());
        }

        return builder;
    }

    internal static WebApplication UseScalar(this WebApplication app, AppSettings appSettings)
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
