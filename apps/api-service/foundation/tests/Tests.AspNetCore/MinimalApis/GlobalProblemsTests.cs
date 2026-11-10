using Aurore.Foundation.AspNetCore.MinimalApis.Configuration;

namespace Tests.AspNetCore.MinimalApis;

[Trait("Feature", "Minimal endpoints")]
public sealed class GlobalProblemsTests
{
    [Fact(DisplayName = "Standard problems are used when no global problems are given")]
    public void UsesStandardProblemsByDefault()
    {
        // Arrange
        // Act
        var problems = MinimalApiConfiguration.ResolveGlobalProblems(null);

        // Assert
        Assert.Same(MinimalApiConfiguration.StandardProblems, problems);
    }

    [Fact(DisplayName = "A custom list replaces the standard problems and drops duplicates")]
    public void CustomListReplacesTheStandardProblems()
    {
        // Arrange
        int[] custom = [401, 500, 500];

        // Act
        var problems = MinimalApiConfiguration.ResolveGlobalProblems(custom);

        // Assert
        Assert.Equal([401, 500], problems);
    }

    [Theory(DisplayName = "Throws when a global problem is not an error status code")]
    [InlineData(200)]
    [InlineData(399)]
    [InlineData(600)]
    public void ThrowsWhenStatusCodeIsNotAnError(int status)
    {
        // Arrange
        int[] custom = [401, status];

        // Act
        var exception = Record.Exception(() => MinimalApiConfiguration.ResolveGlobalProblems(custom));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }
}
