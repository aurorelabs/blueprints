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
    /// <summary>The API version this endpoint belongs to.</summary>
    public int Version { get; }

    /// <summary>The short title shown in the API document.</summary>
    public string Summary { get; }

    /// <summary>A longer description shown in the API document, if any.</summary>
    public string? Description { get; }

    /// <summary>The unique name that identifies the endpoint.</summary>
    public string RouteName { get; }

    /// <summary>Maps the endpoint's route and handler on the group.</summary>
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder);
}
