using Aurore.Foundation.AspNetCore.Configuration;
using Microsoft.Extensions.Configuration;

namespace Tests.AspNetCore.Configuration;

[Trait("Feature", "Strict configuration binding")]
public sealed class ConfigurationBindingTests
{
    [Fact(DisplayName = "Known sections bind and unrelated sections are ignored")]
    public void KnownSectionsBindAndUnrelatedOnesAreIgnored()
    {
        // Arrange
        var configuration = Build(new()
        {
            ["Sample:Name"] = "orders",
            ["Sample:Nested:Retries"] = "3",
            ["Logging:LogLevel:Default"] = "Debug",
            ["AllowedHosts"] = "*",
        });

        // Act
        var settings = configuration.GetStrict<RootSettings>();

        // Assert
        Assert.Equal("orders", settings.Sample.Name);
        Assert.Equal(3, settings.Sample.Nested.Retries);
    }

    [Fact(DisplayName = "Throws when a key inside a known section is not recognized")]
    public void ThrowsWhenAKnownSectionHasAnUnknownKey()
    {
        // Arrange
        var configuration = Build(new()
        {
            ["Sample:Name"] = "orders",
            ["Sample:Nested:Retrys"] = "3",
        });

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => configuration.GetStrict<RootSettings>());

        // Assert
        Assert.Contains("Retrys", error.Message);
    }

    private static IConfiguration Build(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private sealed record RootSettings
    {
        public SampleSettings Sample { get; init; } = new();
    }

    private sealed record SampleSettings
    {
        public string Name { get; init; } = string.Empty;

        public NestedSettings Nested { get; init; } = new();
    }

    private sealed record NestedSettings
    {
        public int Retries { get; init; }
    }
}
