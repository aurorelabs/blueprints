using Aurore.Foundation.AspNetCore.OpenApi.Extensions;

namespace Tests.AspNetCore.OpenApi;

[Trait("Feature", "OpenAPI documents")]
public sealed class DocumentNameTests
{
    [Theory(DisplayName = "Document name follows the API explorer group format")]
    [InlineData(1, "v1")]
    [InlineData(2, "v2")]
    [InlineData(12, "v12")]
    public void DocumentNameMatchesTheGroupNameFormat(int version, string expected)
    {
        Assert.Equal(expected, ScalarExtensions.DocumentName(version));
    }
}
