using System.Linq;
using Aurore.Foundation.AspNetCore.MinimalApis.Abstractions;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Foundation.AspNetCore.MinimalApis;

/// <summary>One endpoint of the resource group TResourceGroup. Override Map to declare the route and handler.</summary>
public abstract class MinimalEndpoint<TResourceGroup> : IMinimalEndpoint
    where TResourceGroup : ResourceGroup
{
    /// <summary>The API version of the endpoint. Defaults to 1.</summary>
    public virtual int Version { get; } = 1;

    /// <summary>An explicit route name. Defaults to one derived from the summary.</summary>
    public virtual string? Name { get; }

    /// <summary>
    /// The short title shown in the API document. Defaults to the class name without "Endpoint", in title case.
    /// </summary>
    public virtual string Summary => GetType().Name.Replace("Endpoint", string.Empty).ToTitleCase();

    /// <summary>A longer description shown in the API document, if any.</summary>
    public virtual string? Description { get; }

    /// <summary>
    /// The unique name that identifies the endpoint: Name when set, otherwise derived from the summary.
    /// </summary>
    public string RouteName => Name ?? string.Concat(Summary.SplitWords().Select(x => x.ToSentenceCase()));

    /// <summary>
    /// Declares the endpoint's route and handler on the group and returns the builder for further conventions.
    /// </summary>
    public abstract RouteHandlerBuilder Map(IEndpointRouteBuilder builder);
}
