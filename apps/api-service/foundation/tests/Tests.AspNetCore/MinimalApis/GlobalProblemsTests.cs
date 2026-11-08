using Aurore.Foundation.AspNetCore.MinimalApis.Configuration;

namespace Tests.AspNetCore.MinimalApis;

[Trait("Feature", "Minimal endpoints")]
public sealed class GlobalProblemsTests
{
    [Fact(DisplayName = "Standard problems are used when no global problems are given")]
    public void UsesStandardProblemsByDefault()
    {
        Assert.Same(MinimalApiConfiguration.StandardProblems, MinimalApiConfiguration.ResolveGlobalProblems(null));
    }

    [Fact(DisplayName = "A custom list replaces the standard problems and drops duplicates")]
    public void CustomListReplacesTheStandardProblems()
    {
        var problems = MinimalApiConfiguration.ResolveGlobalProblems([401, 500, 500]);

        Assert.Equal([401, 500], problems);
    }

    [Theory(DisplayName = "Throws when a global problem is not an error status code")]
    [InlineData(200)]
    [InlineData(399)]
    [InlineData(600)]
    public void ThrowsWhenStatusCodeIsNotAnError(int status)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MinimalApiConfiguration.ResolveGlobalProblems([401, status]));
    }
}
