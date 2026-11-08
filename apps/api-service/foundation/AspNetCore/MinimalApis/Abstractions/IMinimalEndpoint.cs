using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Foundation.AspNetCore.MinimalApis.Abstractions;

/// <summary>
/// What the framework needs from an endpoint of any group, so the catalog can hold endpoints of different groups in one
/// list. Not meant to be implemented directly: derive from <see cref="MinimalEndpoint{TResourceGroup}"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMinimalEndpoint
{
    public int Version { get; }

    public string Summary { get; }

    public string? Description { get; }

    public string RouteName { get; }

    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder);
}
