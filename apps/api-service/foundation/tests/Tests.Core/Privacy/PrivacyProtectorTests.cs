using Aurore.Foundation.Core.Privacy;
using Tests.Shared.Privacy;

namespace Tests.Core.Privacy;

[Trait("Feature", "Privacy")]
public sealed class PrivacyProtectorTests
{
    private const string ClientAddress = "client.address";

    [Fact(DisplayName = "With privacy off, values pass through unchanged")]
    public void DisabledPassesValuesThrough()
    {
        // Arrange
        var protector = PrivacyTestData.DisabledProtector();

        // Act
        var result = protector.Protect(ClientAddress, "203.0.113.7");

        // Assert
        Assert.Equal("203.0.113.7", result);
        Assert.False(protector.IsProtected(ClientAddress));
    }

    [Theory(DisplayName = "The built-in personal fields are encrypted and can be read back")]
    [InlineData("client.address")]
    [InlineData("network.peer.address")]
    [InlineData("user_agent.original")]
    [InlineData("enduser.id")]
    public void DefaultFieldsAreProtected(string field)
    {
        // Arrange
        var protector = PrivacyTestData.Protector();

        // Act
        var token = protector.Protect(field, "some value");

        // Assert
        Assert.NotEqual("some value", token);
        Assert.Equal("some value", protector.Unprotect(token!));
    }

    [Fact(DisplayName = "Fields that are not personal data are left alone")]
    public void OtherFieldsAreUntouched()
    {
        // Arrange
        var protector = PrivacyTestData.Protector();

        // Act
        var result = protector.Protect("server.address", "localhost");

        // Assert
        Assert.Equal("localhost", result);
        Assert.False(protector.IsProtected("server.address"));
    }

    [Fact(DisplayName = "Configured extra fields are protected on top of the defaults, whatever their casing")]
    public void ExtraFieldsAreProtected()
    {
        // Arrange
        var options = PrivacyTestData.Enabled() with { ProtectedFields = ["tenant.id"] };
        var protector = new PrivacyProtector(options);

        // Act
        var extraIsProtected = protector.IsProtected("Tenant.Id");
        var defaultIsProtected = protector.IsProtected(ClientAddress);

        // Assert
        Assert.True(extraIsProtected);
        Assert.True(defaultIsProtected);
    }

    [Fact(DisplayName = "Encrypting the same value twice gives different tokens that both decrypt to it")]
    public void TokensAreRandomized()
    {
        // Arrange
        var protector = PrivacyTestData.Protector();

        // Act
        var first = protector.Protect(ClientAddress, "203.0.113.7");
        var second = protector.Protect(ClientAddress, "203.0.113.7");

        // Assert
        Assert.NotEqual(first, second);
        Assert.Equal("203.0.113.7", protector.Unprotect(first!));
        Assert.Equal("203.0.113.7", protector.Unprotect(second!));
    }

    [Theory(DisplayName = "Empty values are returned as they are, not encrypted")]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyValuesAreReturnedAsTheyAre(string? value)
    {
        // Arrange
        var protector = PrivacyTestData.Protector();

        // Act
        var result = protector.Protect(ClientAddress, value);

        // Assert
        Assert.Equal(value, result);
    }

    [Fact(DisplayName = "After a rotation, tokens made with the old key still decrypt while it is kept as a previous key")]
    public void PreviousKeysDecryptOldTokens()
    {
        // Arrange
        var oldKey = PrivacyTestData.NewKey();
        var token = PrivacyTestData.Protector(oldKey).Protect(ClientAddress, "203.0.113.7");
        var rotated = PrivacyTestData.Protector(PrivacyTestData.NewKey(), oldKey);

        // Act
        var result = rotated.Unprotect(token!);

        // Assert
        Assert.Equal("203.0.113.7", result);
    }

    [Fact(DisplayName = "New values are encrypted with the current key only")]
    public void NewValuesUseTheCurrentKey()
    {
        // Arrange
        var oldKey = PrivacyTestData.NewKey();
        var newKey = PrivacyTestData.NewKey();

        // Act
        var token = PrivacyTestData.Protector(newKey, oldKey).Protect(ClientAddress, "203.0.113.7");

        // Assert
        Assert.Equal("203.0.113.7", PrivacyTestData.Protector(newKey).Unprotect(token!));
        Assert.Null(PrivacyTestData.Protector(oldKey).Unprotect(token!));
    }

    [Fact(DisplayName = "Removing the previous key makes the old tokens unreadable")]
    public void RemovedKeysInvalidateTokens()
    {
        // Arrange
        var token = PrivacyTestData.Protector().Protect(ClientAddress, "203.0.113.7");
        var withoutTheOldKey = PrivacyTestData.Protector();

        // Act
        var result = withoutTheOldKey.Unprotect(token!);

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "A tampered token is rejected")]
    public void TamperedTokensAreRejected()
    {
        // Arrange
        var protector = PrivacyTestData.Protector();
        var token = protector.Protect(ClientAddress, "203.0.113.7")!;
        var tampered = token[..10] + (token[10] == 'A' ? 'B' : 'A') + token[11..];

        // Act
        var result = protector.Unprotect(tampered);

        // Assert
        Assert.Null(result);
    }

    [Theory(DisplayName = "Text that is not a token returns null instead of throwing")]
    [InlineData("")]
    [InlineData("not a token")]
    [InlineData("***")]
    [InlineData("AAAA")]
    public void MalformedTokensReturnNull(string token)
    {
        // Arrange
        var protector = PrivacyTestData.Protector();

        // Act
        var result = protector.Unprotect(token);

        // Assert
        Assert.Null(result);
    }
}
