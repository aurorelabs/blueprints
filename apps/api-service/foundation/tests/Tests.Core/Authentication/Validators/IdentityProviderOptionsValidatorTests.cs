using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Authentication.Validators;

namespace Tests.Core.Authentication.Validators;

[Trait("Feature", "Identity provider validation")]
public sealed class IdentityProviderOptionsValidatorTests
{
    public static TheoryData<string, bool, string> UnsafeProductionSettings => new()
    {
        { "https://localhost:8443/realms/prod", true, "local address" },
        { "http://auth.example.com/realms/prod", true, "https" },
        { "https://auth.example.com/realms/prod", false, "must be true in production" },
    };

    [Fact(DisplayName = "A local http authority without https metadata is valid in development")]
    public void LocalHttpAuthorityIsValidInDevelopment()
    {
        // Arrange
        var options = Options() with { AuthorityUrl = "http://auth.localhost:9999/realms/dev", RequireHttpsMetadata = false };
        var validator = new IdentityProviderOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "Requiring https metadata with an http authority is rejected at startup")]
    public void HttpAuthorityWithRequiredHttpsMetadataIsRejected()
    {
        // Arrange
        var options = Options() with { AuthorityUrl = "http://auth.example.com/realms/dev", RequireHttpsMetadata = true };
        var validator = new IdentityProviderOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "AuthorityUrl" && x.ErrorMessage.Contains("https"));
    }

    [Theory(DisplayName = "Production rejects a local authority, an http authority and disabled https metadata")]
    [MemberData(nameof(UnsafeProductionSettings))]
    public void ProductionRejectsUnsafeSettings(string authority, bool requireHttpsMetadata, string expectedMessage)
    {
        // Arrange
        var options = Options() with { AuthorityUrl = authority, RequireHttpsMetadata = requireHttpsMetadata };
        var validator = new IdentityProviderOptionsValidator(isProduction: true);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains(expectedMessage));
    }

    [Fact(DisplayName = "A secure production configuration is valid")]
    public void SecureProductionConfigurationIsValid()
    {
        // Arrange
        var options = Options() with { AuthorityUrl = "https://auth.example.com/realms/prod", RequireHttpsMetadata = true };
        var validator = new IdentityProviderOptionsValidator(isProduction: true);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory(DisplayName = "A clock skew outside 0 to 300 seconds is rejected")]
    [MemberData(nameof(InvalidClockSkews))]
    public void ClockSkewOutOfRangeIsRejected(int seconds)
    {
        // Arrange
        var options = Options() with { ClockSkewSeconds = seconds };
        var validator = new IdentityProviderOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == "ClockSkewSeconds");
    }

    [Fact(DisplayName = "The default clock skew is 30 seconds and the default provider is Keycloak")]
    public void DefaultsAreThirtySecondsAndKeycloak()
    {
        // Arrange & Act
        var options = Options();

        // Assert
        Assert.Equal(30, options.ClockSkewSeconds);
        Assert.Equal(IdentityProviderKind.Keycloak, options.Provider);
    }

    public static TheoryData<int> InvalidClockSkews => [-1, 301];

    private static IdentityProviderOptions Options()
    {
        return new IdentityProviderOptions { AuthorityUrl = "https://auth.example.com/realms/dev", Audience = "api" };
    }
}
