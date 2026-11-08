using System.Linq;
using Aurore.Foundation.AspNetCore.MinimalApis.Abstractions;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Foundation.AspNetCore.MinimalApis;

public abstract class MinimalEndpoint<TResourceGroup> : IMinimalEndpoint
    where TResourceGroup : ResourceGroup
{
    public virtual int Version { get; } = 1;

    public virtual string? Name { get; }

    public virtual string Summary => GetType().Name.Replace("Endpoint", string.Empty).ToTitleCase();

    public virtual string? Description { get; }

    public string RouteName => Name ?? string.Concat(Summary.SplitWords().Select(x => x.ToSentenceCase()));

    public abstract RouteHandlerBuilder Map(IEndpointRouteBuilder builder);
}
