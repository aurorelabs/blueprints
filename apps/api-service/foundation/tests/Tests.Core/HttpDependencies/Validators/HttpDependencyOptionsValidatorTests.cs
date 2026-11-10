using Aurore.Foundation.Core.HttpDependencies.Models;
using Aurore.Foundation.Core.HttpDependencies.Validators;

namespace Tests.Core.HttpDependencies.Validators;

[Trait("Feature", "HTTP dependency validation")]
public sealed class HttpDependencyOptionsValidatorTests
{
    [Fact(DisplayName = "A dependency with only a base URL is valid, even in production")]
    public void BaseUrlOnlyIsValid()
    {
        // Arrange
        var validator = new HttpDependencyOptionsValidator(isProduction: true);

        // Act
        var result = validator.Validate(Options());

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "A local base URL is accepted in development")]
    public void LocalBaseUrlIsAcceptedInDevelopment()
    {
        // Arrange
        var options = Options() with { BaseUrl = "http://orders.localhost:5002" };
        var validator = new HttpDependencyOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "A local base URL is rejected in production")]
    public void LocalBaseUrlIsRejectedInProduction()
    {
        // Arrange
        var options = Options() with { BaseUrl = "http://orders.localhost:5002" };
        var validator = new HttpDependencyOptionsValidator(isProduction: true);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "BaseUrl" && x.ErrorMessage.Contains("local address"));
    }

    [Fact(DisplayName = "Rejects a base URL that is not absolute")]
    public void RejectsABaseUrlThatIsNotAbsolute()
    {
        // Arrange
        var options = Options() with { BaseUrl = "orders" };
        var validator = new HttpDependencyOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "BaseUrl");
    }

    [Fact(DisplayName = "Rejects an upstream API key without a value")]
    public void RejectsAnApiKeyWithoutAValue()
    {
        // Arrange
        var options = Options() with { ApiKey = new UpstreamApiKeyOptions { HeaderName = "X-Api-Key", Value = string.Empty } };
        var validator = new HttpDependencyOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "ApiKey.Value");
    }

    [Fact(DisplayName = "Rejects client credentials without a client id or a secret")]
    public void RejectsIncompleteClientCredentials()
    {
        // Arrange
        var options = Options() with { ClientCredentials = new ClientCredentialsOptions { TokenUrl = "https://auth.example.com/token" } };
        var validator = new HttpDependencyOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "ClientCredentials.ClientId");
        Assert.Contains(result.Errors, x => x.PropertyName == "ClientCredentials.ClientSecret");
    }

    [Fact(DisplayName = "Production rejects a local token URL")]
    public void ProductionRejectsALocalTokenUrl()
    {
        // Arrange
        var credentials = new ClientCredentialsOptions
        {
            TokenUrl = "http://auth.localhost:9999/token",
            ClientId = "api-service",
            ClientSecret = "secret",
        };
        var validator = new HttpDependencyOptionsValidator(isProduction: true);

        // Act
        var result = validator.Validate(Options() with { ClientCredentials = credentials });

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "ClientCredentials.TokenUrl");
    }

    [Fact(DisplayName = "Rejects a health check with a zero timeout")]
    public void RejectsAHealthCheckWithAZeroTimeout()
    {
        // Arrange
        var options = Options() with { HealthCheck = new HttpDependencyHealthCheckOptions { TimeoutSeconds = 0 } };
        var validator = new HttpDependencyOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "HealthCheck.TimeoutSeconds");
    }

    private static HttpDependencyOptions Options()
    {
        return new HttpDependencyOptions { BaseUrl = "https://orders.example.com" };
    }
}
