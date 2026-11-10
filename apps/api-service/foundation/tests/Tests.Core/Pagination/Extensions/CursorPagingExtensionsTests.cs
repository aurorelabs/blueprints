using System.Buffers.Text;
using System.Text;
using Aurore.Foundation.Core.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;

namespace Tests.Core.Pagination.Extensions;

[Trait("Feature", "Cursor pagination")]
public sealed class CursorPagingExtensionsTests
{
    private static readonly PaginationOptions _options = new() { DefaultPageSize = 4, MaxPageSize = 10 };

    private static readonly SortMap<Item> _sort = SortMap.For<Item>()
        .Allow(x => x.Name)
        .Allow(x => x.Group)
        .Allow(x => x.Level)
        .Allow(x => x.Created)
        .Allow(x => x.Token)
        .ById();

    public static TheoryData<string[]> Orderings => new()
    {
        { [] },
        { ["name"] },
        { ["name:desc"] },
        { ["group", "created:desc"] },
        { ["level:desc", "name"] },
        { ["token"] },
        { ["id:desc"] },
    };

    [Theory(DisplayName = "Following the cursors visits every row once and in order, whatever the ordering (strings, enums, dates, guids, ties)")]
    [MemberData(nameof(Orderings))]
    public void FollowingTheCursorsVisitsEveryRowOnce(string[] orderBy)
    {
        // Arrange
        var query = Items(23);
        var bigger = new PaginationOptions { DefaultPageSize = 4, MaxPageSize = 50 };
        var expected = query.ToPage(new PageRequest { PageSize = 23, OrderBy = orderBy }, bigger, _sort).Value.Items.Select(x => x.Id).ToList();

        // Act
        var visited = Walk(query, new CursorRequest { PageSize = 5, OrderBy = orderBy });

        // Assert
        Assert.Equal(expected, visited);
    }

    [Fact(DisplayName = "The last page has no next cursor, and neither does a list that ends exactly on a page")]
    public void LastPageHasNoNextCursor()
    {
        // Arrange
        var query = Items(8);

        // Act
        var first = query.ToCursorPage(new CursorRequest { PageSize = 4 }, _options, _sort).Value;
        var second = query.ToCursorPage(new CursorRequest { PageSize = 4, Cursor = first.NextCursor }, _options, _sort).Value;

        // Assert
        Assert.NotNull(first.NextCursor);
        Assert.Equal(4, second.Items.Count);
        Assert.Null(second.NextCursor);
    }

    [Fact(DisplayName = "An empty list gives an empty page with no cursor")]
    public void EmptyListGivesNoCursor()
    {
        // Arrange
        var query = Items(0);

        // Act
        var page = query.ToCursorPage(new CursorRequest(), _options, _sort).Value;

        // Assert
        Assert.Empty(page.Items);
        Assert.Null(page.NextCursor);
    }

    [Fact(DisplayName = "The page size may change between calls without invalidating the cursor")]
    public void PageSizeMayChangeBetweenCalls()
    {
        // Arrange
        var query = Items(20);
        var first = query.ToCursorPage(new CursorRequest { PageSize = 3 }, _options, _sort).Value;

        // Act
        var second = query.ToCursorPage(new CursorRequest { PageSize = 7, Cursor = first.NextCursor }, _options, _sort);

        // Assert
        Assert.True(second.IsSuccess);
        Assert.Equal(7, second.Value.Items.Count);
        Assert.Equal(4, second.Value.Items[0].Id);
    }

    [Fact(DisplayName = "The page reports the page size that was used, the default when none was sent")]
    public void PageReportsThePageSize()
    {
        // Arrange
        var query = Items(10);

        // Act
        var page = query.ToCursorPage(new CursorRequest(), _options, _sort).Value;

        // Assert
        Assert.Equal(4, page.PageSize);
    }

    [Fact(DisplayName = "A cursor used with another ordering is refused as a mismatch")]
    public void CursorWithAnotherOrderingIsRefused()
    {
        // Arrange
        var query = Items(20);
        var first = query.ToCursorPage(new CursorRequest { OrderBy = ["name"] }, _options, _sort).Value;

        // Act
        var result = query.ToCursorPage(new CursorRequest { OrderBy = ["name:desc"], Cursor = first.NextCursor }, _options, _sort);

        // Assert
        Assert.Equal("pagination.cursor_mismatch", result.Error!.Code);
    }

    [Fact(DisplayName = "A cursor used with other filters is refused as a mismatch")]
    public void CursorWithOtherFiltersIsRefused()
    {
        // Arrange
        var query = Items(20);
        var first = query.ToCursorPage(new FilteredRequest { Prefix = "a" }, _options, _sort).Value;

        // Act
        var result = query.ToCursorPage(new FilteredRequest { Prefix = "b", Cursor = first.NextCursor }, _options, _sort);

        // Assert
        Assert.Equal("pagination.cursor_mismatch", result.Error!.Code);
    }

    [Fact(DisplayName = "Text that is not a cursor is refused as an invalid cursor")]
    public void GarbageIsRefused()
    {
        // Arrange
        var query = Items(5);

        // Act
        var result = query.ToCursorPage(new CursorRequest { Cursor = "not-a-cursor" }, _options, _sort);

        // Assert
        Assert.Equal("pagination.invalid_cursor", result.Error!.Code);
    }

    [Fact(DisplayName = "A cursor whose values do not fit the ordering is refused instead of failing the query")]
    public void TamperedValuesAreRefused()
    {
        // Arrange
        var query = Items(20);
        var request = new CursorRequest { PageSize = 3 };
        var cursor = query.ToCursorPage(request, _options, _sort).Value.NextCursor!;
        var json = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(cursor));
        var fingerprint = json[(json.IndexOf("\"h\":\"", StringComparison.Ordinal) + 5)..].Split('"')[0];
        var tampered = Base64Url.EncodeToString(Encoding.UTF8.GetBytes($$"""{"h":"{{fingerprint}}","v":["x"]}"""));

        // Act
        var result = query.ToCursorPage(new CursorRequest { PageSize = 3, Cursor = tampered }, _options, _sort);

        // Assert
        Assert.Equal("pagination.invalid_cursor", result.Error!.Code);
    }

    [Fact(DisplayName = "An invalid page size and an unknown ordering are reported together, as for numbered pages")]
    public void ProblemsAreReportedTogether()
    {
        // Arrange
        var query = Items(5);

        // Act
        var result = query.ToCursorPage(new CursorRequest { PageSize = 500, OrderBy = ["secret"] }, _options, _sort);

        // Assert
        Assert.Equal(["orderBy", "pageSize"], result.Error!.Details.Keys.Order());
    }

    [Fact(DisplayName = "Ordering by a nullable property is a mistake of the developer, not of the client")]
    public void NullableOrderingThrows()
    {
        // Arrange
        var query = Items(5);
        var map = SortMap.For<Item>().Allow(x => x.Removed).ById();

        // Act
        var order = () => query.ToCursorPage(new CursorRequest { OrderBy = ["removed"] }, _options, map);

        // Assert
        Assert.Throws<InvalidOperationException>(order);
    }

    [Fact(DisplayName = "A type with no Id cannot be paged with a cursor")]
    public void TypeWithoutIdThrows()
    {
        // Arrange
        var query = new[] { new Keyless("a") }.AsQueryable();
        var map = SortMap.For<Keyless>().DefaultTo(x => x.Name);

        // Act
        var order = () => query.ToCursorPage(new CursorRequest(), _options, map);

        // Assert
        Assert.Throws<InvalidOperationException>(order);
    }

    private static List<int> Walk(IQueryable<Item> query, CursorRequest first)
    {
        var visited = new List<int>();
        var cursor = (string?)null;

        for (var guard = 0; guard < 100; guard++)
        {
            var request = new CursorRequest { PageSize = first.PageSize, OrderBy = first.OrderBy, Cursor = cursor };
            var page = query.ToCursorPage(request, new PaginationOptions { DefaultPageSize = 4, MaxPageSize = 50 }, _sort).Value;

            visited.AddRange(page.Items.Select(x => x.Id));
            cursor = page.NextCursor;

            if (cursor is null)
                return visited;
        }

        throw new InvalidOperationException("The cursors never ended.");
    }

    // Values repeat on purpose (names, groups, levels), and the source is shuffled, so only the ordering decides the result.
    private static IQueryable<Item> Items(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new Item(
                i,
                $"name-{i * 7 % 5}",
                i % 3,
                (Level)(i % 2),
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i * 5 % 11),
                new Guid(i * 13 % 7, 0, 0, [0, 0, 0, 0, 0, 0, 0, 0]),
                null))
            .OrderBy(i => i.Id * 17 % 23)
            .AsQueryable();
    }

    private enum Level
    {
        Low,
        High,
    }

    private sealed record Item(int Id, string Name, int Group, Level Level, DateTimeOffset Created, Guid Token, DateTimeOffset? Removed);

    private sealed record Keyless(string Name);

    private sealed class FilteredRequest : CursorRequest
    {
        public string? Prefix { get; init; }
    }
}
