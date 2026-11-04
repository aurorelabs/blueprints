using System;
using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Blueprints.ApiService.Core.Validators;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aurore.Blueprints.ApiService.App.Api.Configuration;

internal static class AppSettingsConfiguration
{
    internal static IHostApplicationBuilder AddAppSettings(this IHostApplicationBuilder builder, out AppSettings appSettings)
    {
        appSettings = builder.Configuration.Get<AppSettings>() ?? throw new InvalidOperationException("Failed to load appsettings.json correctly.");

        new AppSettingsValidator(builder.Environment).ValidateAndThrow(appSettings);

        builder.Services.AddSingleton(appSettings);

        return builder;
    }
}
