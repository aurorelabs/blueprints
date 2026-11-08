using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Blueprints.ApiService.Core.Validators;
using Aurore.Foundation.AspNetCore.Configuration;
using FluentValidation;
using Microsoft.Extensions.Hosting;

namespace Aurore.Blueprints.ApiService.App.Api.Configuration;

internal static class AppSettingsConfiguration
{
    /// <summary>
    /// Binds and validates AppSettings once at startup. Unknown keys are an error (a typo must not silently fall back to a
    /// default), and the result is deliberately not registered in DI: configuration is passed to the Add*/Use* methods that
    /// need it, so it does not stay reachable for the lifetime of the app.
    /// </summary>
    internal static IHostApplicationBuilder LoadAppSettings(this IHostApplicationBuilder builder, out AppSettings appSettings)
    {
        appSettings = builder.Configuration.GetStrict<AppSettings>();

        new AppSettingsValidator(builder.Environment).ValidateAndThrow(appSettings);

        return builder;
    }
}
