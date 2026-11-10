using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;

namespace Tests.Core.Pagination.Extensions;

[Trait("Feature", "Pagination")]
public sealed class PagingExtensionsTests
{
    private static readonly PaginationOptions _options = new() { DefaultPageSize = 4, MaxPageSize = 10 };

    private static readonly SortMap<Row> _sort = SortMap.For<Row>()
        .Allow(x => x.Id)
        .Allow(x => x.Group)
        .Allow(x => x.Created)
        .DefaultTo(x => x.Id);

    public static TheoryData<int?, int?, string> InvalidWindows => new()
    {
        { 0, null, "page" },
        { -3, null, "page" },
        { null, 0, "pageSize" },
        { null, 11, "pageSize" },
        { int.MaxValue, 10, "page" },
    };

    public static TheoryData<string[], string> InvalidOrderings => new()
    {
        { ["secret"], "cannot be ordered by" },
        { ["id:sideways"], "is not valid" },
        { ["group", "group:desc"], "more than once" },
    };

    [Fact(DisplayName = "Without page, page size or ordering the first page comes in the default size and default order")]
    public void DefaultsApplyWhenTheRequestSendsNothing()
    {
        // Arrange
        var query = Rows(10);

        // Act
        var result = query.ToPage(new PageRequest(), _options, _sort);

        // Assert
        var page = result.Value;
        Assert.Equal([1, 2, 3, 4], page.Items.Select(x => x.Id));
        Assert.Equal((1, 4, 10, 3), (page.Number, page.PageSize, page.TotalItems, page.TotalPages));
    }

    [Fact(DisplayName = "A later page holds the items after the earlier pages, and the last page may be short")]
    public void LaterPagesAreCutFromTheOrderedList()
    {
        // Arrange
        var query = Rows(10);

        // Act
        var result = query.ToPage(new PageRequest { Page = 3, PageSize = 4 }, _options, _sort);

        // Assert
        Assert.Equal([9, 10], result.Value.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "A page past the end is empty but still reports the totals")]
    public void PagePastTheEndIsEmpty()
    {
        // Arrange
        var query = Rows(10);

        // Act
        var result = query.ToPage(new PageRequest { Page = 9 }, _options, _sort);

        // Assert
        Assert.Empty(result.Value.Items);
        Assert.Equal(10, result.Value.TotalItems);
    }

    [Fact(DisplayName = "Several orderings apply in the order given, each in its own direction")]
    public void OrderingsApplyInOrder()
    {
        // Arrange
        var query = Rows(6);
        var request = new PageRequest { PageSize = 6, OrderBy = ["group:desc", "id"] };

        // Act
        var result = query.ToPage(request, _options, _sort);

        // Assert
        Assert.Equal([2, 4, 6, 1, 3, 5], result.Value.Items.Select(x => x.Id));
    }

    [Theory(DisplayName = "An invalid page or page size is rejected with the offending parameter named")]
    [MemberData(nameof(InvalidWindows))]
    public void InvalidWindowsAreRejected(int? page, int? pageSize, string field)
    {
        // Arrange
        var query = Rows(3);

        // Act
        var result = query.ToPage(new PageRequest { Page = page, PageSize = pageSize }, _options, _sort);

        // Assert
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Contains(field, result.Error.Details.Keys);
    }

    [Theory(DisplayName = "An unknown, malformed or repeated ordering is rejected")]
    [MemberData(nameof(InvalidOrderings))]
    public void InvalidOrderingsAreRejected(string[] orderBy, string expectedMessage)
    {
        // Arrange
        var query = Rows(3);

        // Act
        var result = query.ToPage(new PageRequest { OrderBy = orderBy }, _options, _sort);

        // Assert
        Assert.Contains(expectedMessage, Assert.Single(result.Error!.Details["orderBy"]));
    }

    [Fact(DisplayName = "Ordering by a field with no map is rejected, so clients can only sort by what was allowed")]
    public void OrderingWithoutAMapIsRejected()
    {
        // Arrange
        var query = Rows(3);

        // Act
        var result = query.ToPage(new PageRequest { OrderBy = ["id"] }, _options);

        // Assert
        Assert.Contains("orderBy", result.Error!.Details.Keys);
    }

    [Fact(DisplayName = "Every problem of a request comes back together")]
    public void ProblemsAreReportedTogether()
    {
        // Arrange
        var query = Rows(3);
        var request = new PageRequest { Page = 0, PageSize = 500, OrderBy = ["secret"] };

        // Act
        var result = query.ToPage(request, _options, _sort);

        // Assert
        Assert.Equal(["orderBy", "page", "pageSize"], result.Error!.Details.Keys.Order());
    }

    [Fact(DisplayName = "A field is allowed under its property name in camelCase, or under the name given")]
    public void NamesAreInferredOrGiven()
    {
        // Arrange & Act
        var map = SortMap.For<Row>()
            .Allow(x => x.Created)
            .Allow(x => x.Group, "customerGroup");

        // Assert
        Assert.Equal(["created", "customerGroup"], map.Fields.Order());
    }

    [Fact(DisplayName = "A path through another object has no obvious name, so it must be given one")]
    public void NestedPathsNeedAName()
    {
        // Arrange
        var map = SortMap.For<Nested>();

        // Act
        var withoutName = () => map.Allow(x => x.Row.Id);
        map.Allow(x => x.Row.Id, "rowId");

        // Assert
        Assert.Throws<ArgumentException>(withoutName);
        Assert.Contains("rowId", map.Fields);
    }

    [Fact(DisplayName = "A default given by property also allows that field, and takes its direction")]
    public void DefaultByPropertyAllowsTheFieldAndKeepsItsDirection()
    {
        // Arrange
        var map = SortMap.For<Row>().DefaultTo(x => x.Id, SortDirection.Desc);

        // Act
        var result = Rows(3).ToPage(new PageRequest(), _options, map);

        // Assert
        Assert.Contains("id", map.Fields);
        Assert.Equal([3, 2, 1], result.Value.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "Without a map the page is ordered by id")]
    public void NoMapOrdersById()
    {
        // Arrange
        var query = Rows(5).Reverse();

        // Act
        var result = query.ToPage(new PageRequest(), _options);

        // Assert
        Assert.Equal([1, 2, 3, 4], result.Value.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "A map with no default orders by id, and ById makes ascending or descending id the default")]
    public void MapWithoutDefaultOrdersByIdAndByIdSetsTheDirection()
    {
        // Arrange
        var query = Rows(3).Reverse();
        var plain = SortMap.For<Row>().Allow(x => x.Group);
        var descending = SortMap.For<Row>().ById(SortDirection.Desc);

        // Act
        var byDefault = query.ToPage(new PageRequest(), _options, plain);
        var byDescendingId = query.ToPage(new PageRequest(), _options, descending);

        // Assert
        Assert.Equal([1, 2, 3], byDefault.Value.Items.Select(x => x.Id));
        Assert.Equal([3, 2, 1], byDescendingId.Value.Items.Select(x => x.Id));
        Assert.Contains("id", descending.Fields);
    }

    [Fact(DisplayName = "Rows that tie on the requested ordering keep the same order by id, wherever the source puts them")]
    public void IdBreaksTies()
    {
        // Arrange
        var query = Rows(6).Reverse();
        var request = new PageRequest { PageSize = 6, OrderBy = ["group"] };

        // Act
        var result = query.ToPage(request, _options, _sort);

        // Assert
        Assert.Equal([1, 3, 5, 2, 4, 6], result.Value.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "When the request already orders by id, no second id ordering is added after it")]
    public void RequestedIdOrderIsNotOverridden()
    {
        // Arrange
        var query = Rows(4);
        var request = new PageRequest { OrderBy = ["id:desc"] };

        // Act
        var result = query.ToPage(request, _options, _sort);

        // Assert
        Assert.Equal([4, 3, 2, 1], result.Value.Items.Select(x => x.Id));
    }

    [Fact(DisplayName = "A type with no Id cannot be paged without a default of its own")]
    public void TypeWithoutIdNeedsADefault()
    {
        // Arrange
        var query = new[] { new Keyless("b"), new Keyless("a") }.AsQueryable();
        var map = SortMap.For<Keyless>().DefaultTo(x => x.Name);

        // Act
        var withoutMap = () => query.ToPage(new PageRequest(), _options);
        var withMap = query.ToPage(new PageRequest(), _options, map);

        // Assert
        Assert.Throws<InvalidOperationException>(withoutMap);
        Assert.Equal(["a", "b"], withMap.Value.Items.Select(x => x.Name));
    }

    [Fact(DisplayName = "ById on a type with no Id is a programming mistake reported at once")]
    public void ByIdNeedsAnId()
    {
        // Arrange
        var map = SortMap.For<Keyless>();

        // Act
        var declare = () => map.ById();

        // Assert
        Assert.Throws<InvalidOperationException>(declare);
    }

    [Fact(DisplayName = "A map default must name an allowed field")]
    public void DefaultMustBeAllowed()
    {
        // Arrange
        var map = SortMap.For<Row>().Allow(x => x.Id);

        // Act
        var declare = () => map.DefaultTo("missing");

        // Assert
        Assert.Throws<ArgumentException>(declare);
    }

    [Fact(DisplayName = "A date range starts at its From, inclusive, and stops before its To, exclusive")]
    public void RangesAreHalfOpen()
    {
        // Arrange
        var query = Rows(10);
        var filter = new Timestamps { CreatedFrom = Day(3), CreatedTo = Day(6) };

        // Act
        var result = query.WhereTimestamps(filter, x => x.Created);

        // Assert
        Assert.Equal([3, 4, 5], result.Value.Select(x => x.Id));
    }

    [Fact(DisplayName = "A range with only one bound is open on the other side")]
    public void OneBoundIsEnough()
    {
        // Arrange
        var query = Rows(5);
        var filter = new Timestamps { CreatedTo = Day(3) };

        // Act
        var result = query.WhereTimestamps(filter, x => x.Created);

        // Assert
        Assert.Equal([1, 2], result.Value.Select(x => x.Id));
    }

    [Fact(DisplayName = "A range whose end is not after its start is rejected, naming the end")]
    public void BackwardsRangeIsRejected()
    {
        // Arrange
        var query = Rows(5);
        var filter = new Timestamps { CreatedFrom = Day(4), CreatedTo = Day(4) };

        // Act
        var result = query.WhereTimestamps(filter, x => x.Created);

        // Assert
        Assert.Contains("createdTo", result.Error!.Details.Keys);
    }

    [Fact(DisplayName = "A request carrying a range nobody can apply is a programming mistake, not a client error")]
    public void MissingSelectorThrows()
    {
        // Arrange
        var query = Rows(5);
        var filter = new Timestamps { UpdatedFrom = Day(1) };

        // Act
        var apply = () => query.WhereTimestamps(filter, x => x.Created);

        // Assert
        Assert.Throws<InvalidOperationException>(apply);
    }

    private static IQueryable<Row> Rows(int count)
    {
        return Enumerable.Range(1, count).Select(i => new Row(i, i % 2 == 0 ? 2 : 1, Day(i))).AsQueryable();
    }

    private static DateTimeOffset Day(int day)
    {
        return new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(day);
    }

    private sealed record Row(int Id, int Group, DateTimeOffset Created);

    private sealed record Nested(Row Row);

    private sealed record Keyless(string Name);

    private sealed class Timestamps : ITimestampFilter
    {
        public DateTimeOffset? CreatedFrom { get; init; }

        public DateTimeOffset? CreatedTo { get; init; }

        public DateTimeOffset? UpdatedFrom { get; init; }

        public DateTimeOffset? UpdatedTo { get; init; }
    }
}
