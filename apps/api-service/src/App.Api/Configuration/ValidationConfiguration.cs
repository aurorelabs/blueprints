using System.Globalization;
using Aurore.Blueprints.ApiService.Core;
using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Blueprints.ApiService.Features;
using Aurore.Foundation.AspNetCore.Configuration;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Blueprints.ApiService.App.Api.Configuration;

internal static class ValidationConfiguration
{
    internal static IServiceCollection AddAppValidators(this IServiceCollection services, AppSettings appSettings)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo(appSettings.AppInformation.Locale);
        // ValidationErrorOptions.LocationFormat = ValidationErrorLocationFormat.FieldOnly;

        return services.AddAuroreValidators(
            [typeof(Program).Assembly, typeof(ICore).Assembly, typeof(IFeatures).Assembly],
            includeInternalTypes: true);
    }
}
