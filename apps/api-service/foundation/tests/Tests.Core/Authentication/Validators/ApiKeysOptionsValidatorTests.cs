using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Authentication.Validators;

namespace Tests.Core.Authentication.Validators;

[Trait("Feature", "API key validation")]
public sealed class ApiKeysOptionsValidatorTests
{
    private const string LongKey = "0123456789abcdef0123456789abcdef";

    [Fact(DisplayName = "No clients at all is valid, so API keys stay switched off")]
    public void NoClientsIsValid()
    {
        // Arrange
        var validator = new ApiKeysOptionsValidator(isProduction: true);

        // Act
        var result = validator.Validate(new ApiKeysOptions());

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "An empty header name is rejected")]
    public void EmptyHeaderNameIsRejected()
    {
        // Arrange
        var options = new ApiKeysOptions { HeaderName = string.Empty };
        var validator = new ApiKeysOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "HeaderName");
    }

    [Fact(DisplayName = "A client with an empty key is reported under the client's name")]
    public void EmptyKeyIsReportedWithTheClientName()
    {
        // Arrange
        var options = Options(("billing", string.Empty));
        var validator = new ApiKeysOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "Clients:billing:Key");
    }

    [Fact(DisplayName = "Two clients sharing a key are rejected, since the key could not tell them apart")]
    public void SharedKeysAreRejected()
    {
        // Arrange
        var options = Options(("billing", LongKey), ("shipping", LongKey));
        var validator = new ApiKeysOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "Clients" && x.ErrorMessage.Contains("billing") && x.ErrorMessage.Contains("shipping"));
    }

    [Fact(DisplayName = "A short key is rejected in production but accepted in development")]
    public void ShortKeysAreRejectedOnlyInProduction()
    {
        // Arrange
        var options = Options(("billing", "short"));

        // Act
        var production = new ApiKeysOptionsValidator(isProduction: true).Validate(options);
        var development = new ApiKeysOptionsValidator(isProduction: false).Validate(options);

        // Assert
        Assert.Contains(production.Errors, x => x.PropertyName == "Clients:billing:Key");
        Assert.True(development.IsValid);
    }

    private static ApiKeysOptions Options(params (string Name, string Key)[] clients)
    {
        return new ApiKeysOptions
        {
            Clients = clients.ToDictionary(x => x.Name, x => new ApiKeyClientOptions { Key = x.Key }),
        };
    }
}
