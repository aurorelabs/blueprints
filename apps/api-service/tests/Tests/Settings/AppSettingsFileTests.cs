using Aurore.Blueprints.ApiService.Core.Settings;
using Aurore.Blueprints.ApiService.Core.Validators;
using Aurore.Foundation.AspNetCore.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Tests.Settings;

[Trait("Feature", "App settings")]
public sealed class AppSettingsFileTests
{
    [Fact(DisplayName = "appsettings.json binds without unknown keys and is valid in development")]
    public void DevelopmentSettingsAreValid()
    {
        var settings = Load(production: false);

        var result = new AppSettingsValidator(new TestEnvironment(Environments.Development)).Validate(settings);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}")));
    }

    [Fact(DisplayName = "Production refuses the development defaults until they are overridden")]
    public void ProductionRefusesTheDevelopmentDefaults()
    {
        var settings = Load(production: true);

        var result = new AppSettingsValidator(new TestEnvironment(Environments.Production)).Validate(settings);

        var failed = result.Errors.Select(x => x.PropertyName).ToList();

        Assert.Contains("IdentityProvider.AuthorityUrl", failed);
        Assert.Contains("Telemetry.Otlp.Endpoint", failed);
        Assert.Contains("HttpDependencies:Orders:BaseUrl", failed);
        Assert.Contains("HttpDependencies:Orders:ClientCredentials.TokenUrl", failed);
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains("Production API keys"));
    }

    private static AppSettings Load(bool production)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false);

        if (production)
            configuration.AddJsonFile("appsettings.Production.json", optional: false);

        return configuration.Build().GetStrict<AppSettings>();
    }
}
