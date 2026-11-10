using Aurore.Foundation.Core.Pagination.Models;

namespace Tests.Core.Pagination.Models;

[Trait("Feature", "Pagination")]
public sealed class PageTests
{
    public static TheoryData<int, int, int> TotalPageCases => new()
    {
        { 0, 10, 0 },
        { 1, 10, 1 },
        { 10, 10, 1 },
        { 11, 10, 2 },
        { 45, 20, 3 },
    };

    [Theory(DisplayName = "The total pages round up, and are zero when there is nothing to list")]
    [MemberData(nameof(TotalPageCases))]
    public void TotalPagesRoundUp(int totalItems, int pageSize, int expected)
    {
        // Arrange & Act
        var page = Page<int>.Create([], 1, pageSize, totalItems);

        // Assert
        Assert.Equal(expected, page.TotalPages);
    }

    [Fact(DisplayName = "Mapping a page transforms the items and keeps the numbers")]
    public void MapKeepsTheNumbers()
    {
        // Arrange
        var page = Page<int>.Create([1, 2], 3, 2, 10);

        // Act
        var mapped = page.Map(x => $"#{x}");

        // Assert
        Assert.Equal(["#1", "#2"], mapped.Items);
        Assert.Equal((3, 2, 10, 5), (mapped.Number, mapped.PageSize, mapped.TotalItems, mapped.TotalPages));
    }
}
