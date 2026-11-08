using Aurore.Foundation.Core.HttpDependencies.Models;
using Aurore.Foundation.Core.HttpDependencies.Validators;

namespace Tests.Core.HttpDependencies;

[Trait("Feature", "HTTP dependency validation")]
public sealed class HttpDependencyOptionsValidatorTests
{
    [Fact(DisplayName = "A dependency with only a base URL is valid")]
    public void BaseUrlOnlyIsValid()
    {
        Assert.True(new HttpDependencyOptionsValidator(isProduction: true).Validate(Options()).IsValid);
    }

    [Fact(DisplayName = "A local base URL is accepted in development but rejected in production")]
    public void LocalBaseUrlIsOnlyAcceptedInDevelopment()
    {
        var options = Options() with { BaseUrl = "http://orders.localhost:5002" };

        Assert.True(new HttpDependencyOptionsValidator(isProduction: false).Validate(options).IsValid);

        var result = new HttpDependencyOptionsValidator(isProduction: true).Validate(options);

        Assert.Contains(result.Errors, x => x.PropertyName == "BaseUrl" && x.ErrorMessage.Contains("local address"));
    }

    [Fact(DisplayName = "Rejects a base URL that is not absolute")]
    public void RejectsABaseUrlThatIsNotAbsolute()
    {
        var result = new HttpDependencyOptionsValidator(isProduction: false).Validate(Options() with { BaseUrl = "orders" });

        Assert.Contains(result.Errors, x => x.PropertyName == "BaseUrl");
    }

    [Fact(DisplayName = "Rejects an upstream API key without a value")]
    public void RejectsAnApiKeyWithoutAValue()
    {
        var options = Options() with { ApiKey = new UpstreamApiKeyOptions { HeaderName = "X-Api-Key", Value = string.Empty } };

        var result = new HttpDependencyOptionsValidator(isProduction: false).Validate(options);

        Assert.Contains(result.Errors, x => x.PropertyName == "ApiKey.Value");
    }

    [Fact(DisplayName = "Rejects client credentials without a client id or a secret")]
    public void RejectsIncompleteClientCredentials()
    {
        var options = Options() with { ClientCredentials = new ClientCredentialsOptions { TokenUrl = "https://auth.example.com/token" } };

        var result = new HttpDependencyOptionsValidator(isProduction: false).Validate(options);

        Assert.Contains(result.Errors, x => x.PropertyName == "ClientCredentials.ClientId");
        Assert.Contains(result.Errors, x => x.PropertyName == "ClientCredentials.ClientSecret");
    }

    [Fact(DisplayName = "Production rejects a local token URL")]
    public void ProductionRejectsALocalTokenUrl()
    {
        var credentials = new ClientCredentialsOptions
        {
            TokenUrl = "http://auth.localhost:9999/token",
            ClientId = "api-service",
            ClientSecret = "secret",
        };

        var result = new HttpDependencyOptionsValidator(isProduction: true).Validate(Options() with { ClientCredentials = credentials });

        Assert.Contains(result.Errors, x => x.PropertyName == "ClientCredentials.TokenUrl");
    }

    [Fact(DisplayName = "Rejects a health check with a zero timeout")]
    public void RejectsAHealthCheckWithAZeroTimeout()
    {
        var options = Options() with { HealthCheck = new HttpDependencyHealthCheckOptions { TimeoutSeconds = 0 } };

        var result = new HttpDependencyOptionsValidator(isProduction: false).Validate(options);

        Assert.Contains(result.Errors, x => x.PropertyName == "HealthCheck.TimeoutSeconds");
    }

    private static HttpDependencyOptions Options()
    {
        return new HttpDependencyOptions { BaseUrl = "https://orders.example.com" };
    }
}
