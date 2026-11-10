using Aurore.Foundation.Core.Pagination.Models;

namespace Tests.Core.Pagination.Models;

[Trait("Feature", "Pagination")]
public sealed class SortItemTests
{
    public static TheoryData<string, string, SortDirection> Valid => new()
    {
        { "name", "name", SortDirection.Asc },
        { "name:asc", "name", SortDirection.Asc },
        { "name:desc", "name", SortDirection.Desc },
        { " Name : DESC ", "Name", SortDirection.Desc },
    };

    public static TheoryData<string> Invalid => new() { string.Empty, "  ", ":desc", "name:sideways", "name:asc:desc" };

    [Theory(DisplayName = "An ordering is read as field and direction, defaulting to ascending")]
    [MemberData(nameof(Valid))]
    public void ValidOrderingsAreParsed(string text, string field, SortDirection direction)
    {
        // Arrange & Act
        var parsed = SortItem.TryParse(text, out var item);

        // Assert
        Assert.True(parsed);
        Assert.Equal(new SortItem(field, direction), item);
    }

    [Theory(DisplayName = "An empty, direction-only or unknown-direction ordering is rejected")]
    [MemberData(nameof(Invalid))]
    public void InvalidOrderingsAreRejected(string text)
    {
        // Arrange & Act
        var parsed = SortItem.TryParse(text, out _);

        // Assert
        Assert.False(parsed);
    }

    [Fact(DisplayName = "A missing ordering (null) is rejected")]
    public void MissingOrderingIsRejected()
    {
        // Arrange & Act
        var parsed = SortItem.TryParse(null, out _);

        // Assert
        Assert.False(parsed);
    }
}
