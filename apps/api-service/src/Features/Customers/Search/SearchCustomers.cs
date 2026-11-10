using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Core.Features;

namespace Aurore.Blueprints.ApiService.Features.Customers.Search;

/// <summary>
/// The request is both the HTTP contract and the input of the feature: paging, ordering and date ranges come from the base
/// class, and the filters of this query are added here.
/// </summary>
public sealed class SearchCustomersRequest : TimestampPageRequest
{
    /// <summary>Customers whose name contains this text.</summary>
    public string? Name { get; init; }
}

/// <summary>One customer in the search results.</summary>
public sealed record CustomerItem(int Id, string Name, string Email, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>Searches customers, one page at a time.</summary>
public sealed class SearchCustomers(PaginationOptions pagination) : Feature<SearchCustomersRequest, Page<CustomerItem>>
{
    private static readonly SortMap<CustomerItem> _sort = SortMap.For<CustomerItem>()
        .Allow(x => x.Name)
        .Allow(x => x.Email)
        .Allow(x => x.CreatedAt)
        .ById();

    protected override ValueTask<Result<Page<CustomerItem>>> ExecuteAsync(SearchCustomersRequest request, CancellationToken cancellationToken)
    {
        // TODO(EF Core): query the DbContext (db.Customers) instead of this empty source.
        var customers = Enumerable.Empty<CustomerItem>().AsQueryable();

        if (string.IsNullOrWhiteSpace(request.Name) is false)
            customers = customers.Where(x => x.Name.Contains(request.Name));

        var filtered = customers.WhereTimestamps(request, x => x.CreatedAt, x => x.UpdatedAt);

        var result = filtered.IsFailure
            ? (Result<Page<CustomerItem>>)filtered.Error!
            : filtered.Value.ToPage(request, pagination, _sort);

        return ValueTask.FromResult(result);
    }
}
