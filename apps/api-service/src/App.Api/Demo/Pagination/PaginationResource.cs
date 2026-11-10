using System;
using System.Linq;
using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Demo.Pagination;

/// <summary>
/// Numbered pages. Try: ?page=2&amp;pageSize=5, ?orderBy=name:desc&amp;orderBy=id, ?createdFrom=2026-01-10T00:00:00Z,
/// then the errors: ?pageSize=1000, ?orderBy=secret, ?createdFrom=2026-02-01T00:00:00Z&amp;createdTo=2026-01-01T00:00:00Z.
/// </summary>
internal sealed class PaginationResource() : DemoResource("pagination", "Pagination")
{
    public override bool RequireAuthorization => false;
}

/// <summary>The request carries the paging, the ordering and the date ranges; the filter of this query is the name.</summary>
internal sealed class SearchDemoPeopleRequest : TimestampPageRequest
{
    public string? Name { get; init; }
}

internal sealed class GetDemoPeopleEndpoint : MinimalEndpoint<PaginationResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("people", Handle)
            .ProducesPage<DemoPerson>();

        static Results<Ok<Page<DemoPerson>>, ProblemHttpResult> Handle([AsParameters] SearchDemoPeopleRequest request, PaginationOptions options)
        {
            return DemoPeople.Search(request, options).ToHttpResult();
        }
    }
}

/// <summary>The same list as an infinite scroll: send the nextCursor of each response back to get the following page.</summary>
internal sealed class StreamDemoPeopleRequest : TimestampCursorRequest
{
    public string? Name { get; init; }
}

internal sealed class GetDemoPeopleStreamEndpoint : MinimalEndpoint<PaginationResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet("people/stream", Handle)
            .ProducesCursorPage<DemoPerson>();

        static Results<Ok<CursorPage<DemoPerson>>, ProblemHttpResult> Handle([AsParameters] StreamDemoPeopleRequest request, PaginationOptions options)
        {
            return DemoPeople.Stream(request, options).ToHttpResult();
        }
    }
}

internal sealed record DemoPerson(int Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>A stand-in for a feature: 45 people in memory, queried through the paging helpers.</summary>
internal static class DemoPeople
{
    private static readonly SortMap<DemoPerson> _sort = SortMap.For<DemoPerson>()
        .Allow(x => x.Name)
        .Allow(x => x.CreatedAt)
        .ById();

    private static readonly DemoPerson[] _people =
    [
        .. Enumerable.Range(1, 45).Select(i => new DemoPerson(
            i,
            $"Person {(char)('A' + (i % 26))}{i:00}",
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i),
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i))),
    ];

    public static Result<Page<DemoPerson>> Search(SearchDemoPeopleRequest request, PaginationOptions options)
    {
        var filtered = Filter(request.Name, request);

        return filtered.IsFailure ? filtered.Error! : filtered.Value.ToPage(request, options, _sort);
    }

    public static Result<CursorPage<DemoPerson>> Stream(StreamDemoPeopleRequest request, PaginationOptions options)
    {
        var filtered = Filter(request.Name, request);

        return filtered.IsFailure ? filtered.Error! : filtered.Value.ToCursorPage(request, options, _sort);
    }

    private static Result<IQueryable<DemoPerson>> Filter(string? name, ITimestampFilter timestamps)
    {
        var query = _people.AsQueryable();

        if (string.IsNullOrWhiteSpace(name) is false)
            query = query.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        return query.WhereTimestamps(timestamps, x => x.CreatedAt, x => x.UpdatedAt);
    }
}
