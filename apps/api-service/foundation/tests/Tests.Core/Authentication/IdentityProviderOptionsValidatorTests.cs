using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Authentication.Validators;

namespace Tests.Core.Authentication;

[Trait("Feature", "Identity provider validation")]
public sealed class IdentityProviderOptionsValidatorTests
{
    [Fact(DisplayName = "A local http authority without https metadata is valid in development")]
    public void LocalHttpAuthorityIsValidInDevelopment()
    {
        var options = Options() with { AuthorityUrl = "http://auth.localhost:9999/realms/dev", RequireHttpsMetadata = false };

        Assert.True(new IdentityProviderOptionsValidator(isProduction: false).Validate(options).IsValid);
    }

    [Fact(DisplayName = "Requiring https metadata with an http authority is rejected at startup")]
    public void HttpAuthorityWithRequiredHttpsMetadataIsRejected()
    {
        var options = Options() with { AuthorityUrl = "http://auth.example.com/realms/dev", RequireHttpsMetadata = true };

        var result = new IdentityProviderOptionsValidator(isProduction: false).Validate(options);

        Assert.Contains(result.Errors, x => x.PropertyName == "AuthorityUrl" && x.ErrorMessage.Contains("https"));
    }

    [Theory(DisplayName = "Production rejects a local authority, an http authority and disabled https metadata")]
    [InlineData("https://localhost:8443/realms/prod", true, "local address")]
    [InlineData("http://auth.example.com/realms/prod", true, "https")]
    [InlineData("https://auth.example.com/realms/prod", false, "must be true in production")]
    public void ProductionRejectsUnsafeSettings(string authority, bool requireHttpsMetadata, string expectedMessage)
    {
        var options = Options() with { AuthorityUrl = authority, RequireHttpsMetadata = requireHttpsMetadata };

        var result = new IdentityProviderOptionsValidator(isProduction: true).Validate(options);

        Assert.Contains(result.Errors, x => x.ErrorMessage.Contains(expectedMessage));
    }

    [Fact(DisplayName = "A secure production configuration is valid")]
    public void SecureProductionConfigurationIsValid()
    {
        var options = Options() with { AuthorityUrl = "https://auth.example.com/realms/prod", RequireHttpsMetadata = true };

        Assert.True(new IdentityProviderOptionsValidator(isProduction: true).Validate(options).IsValid);
    }

    private static IdentityProviderOptions Options()
    {
        return new IdentityProviderOptions { AuthorityUrl = "https://auth.example.com/realms/dev", Audience = "api" };
    }
}
