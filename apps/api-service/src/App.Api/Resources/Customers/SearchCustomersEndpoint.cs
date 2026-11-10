using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Features.Customers.Search;
using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.Pagination.Extensions;
using Aurore.Foundation.Core.Pagination.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Blueprints.ApiService.App.Api.Resources.Customers;

/// <summary>
/// Lists customers one page at a time. Order with orderBy=name:desc (fields: name, email, createdAt, id) and narrow with
/// name, createdFrom/createdTo and updatedFrom/updatedTo (from inclusive, to exclusive).
/// </summary>
internal sealed class SearchCustomersEndpoint : MinimalEndpoint<CustomersResource>
{
    public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
    {
        return builder
            .MapGet(string.Empty, Handle)
            .ProducesPage<CustomerItem>();

        static async Task<Results<Ok<Page<CustomerItem>>, ProblemHttpResult>> Handle(
            [AsParameters] SearchCustomersRequest request,
            SearchCustomers feature,
            CancellationToken cancellationToken)
        {
            var result = await feature.HandleAsync(request, cancellationToken);

            return result.ToHttpResult();
        }
    }
}
