using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Pagination.Validators;

namespace Tests.Core.Pagination.Validators;

[Trait("Feature", "Pagination")]
public sealed class PaginationOptionsValidatorTests
{
    public static TheoryData<int, int> Invalid => new()
    {
        { 0, 100 },
        { 20, 0 },
        { 50, 20 },
    };

    [Fact(DisplayName = "The default pagination settings are valid")]
    public void DefaultsAreValid()
    {
        // Arrange
        var validator = new PaginationOptionsValidator();

        // Act
        var result = validator.Validate(new PaginationOptions());

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory(DisplayName = "A zero size, or a default larger than the maximum, is rejected at startup")]
    [MemberData(nameof(Invalid))]
    public void InvalidSizesAreRejected(int defaultSize, int maxSize)
    {
        // Arrange
        var options = new PaginationOptions { DefaultPageSize = defaultSize, MaxPageSize = maxSize };
        var validator = new PaginationOptionsValidator();

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.False(result.IsValid);
    }
}
