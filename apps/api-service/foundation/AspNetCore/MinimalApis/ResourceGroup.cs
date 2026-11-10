using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Routing;

namespace Aurore.Foundation.AspNetCore.MinimalApis;

/// <summary>
/// Minimal API structure: a ResourceGroup is one URL group and a MinimalEndpoint&lt;TGroup&gt; is one endpoint class.
/// AddAuroreMinimalEndpoints&lt;T&gt;() discovers and validates them, and MapAuroreMinimalEndpoints() maps them.
/// Conventions stack from the outside in; each level only adds:
///
///   application  MapAuroreMinimalEndpoints(catalog, api =&gt; ...)      every endpoint (global problems: StandardProblems unless replaced)
///   resource     ResourceGroup.Problems / Configure(group =&gt; ...)     every endpoint in that group
///   endpoint     Map(...) =&gt; ... .ProducesProblem(...)                that endpoint only
///
/// Authorization: groups require an authenticated caller by default, so a new endpoint without auth fails closed.
/// Narrow it in the group's Configure (group.RequireScopes("api.write")) or per endpoint;
/// requirements from every level must pass. Opt out with RequireAuthorization =&gt; false (group) or .AllowAnonymous() (endpoint).
///
/// Groups and endpoints are created once at startup with their parameterless constructors; handler parameters (services,
/// route values, body) are bound per request by the framework, so the scan has no per-request cost.
/// </summary>
public abstract class ResourceGroup
{
    /// <summary>The class name without "Resource".</summary>
    public virtual string TypeName => GetType().Name.Replace("Resource", string.Empty);

    /// <summary>The URL segment of the group. Defaults to the type name in slug case.</summary>
    public virtual string Name => TypeName.ToSlugCase();

    /// <summary>Whether the group's URL starts with /api.</summary>
    public virtual bool PrefixWithApi => false;

    /// <summary>
    /// Whether callers must be authenticated. True by default, so a new endpoint without authorization fails closed.
    /// </summary>
    public virtual bool RequireAuthorization => true;

    /// <summary>The tags that group the endpoints in the API document. Defaults to the type name.</summary>
    public virtual string[] Tags => [TypeName];

    /// <summary>The API versions the group exposes. Defaults to version 1.</summary>
    public virtual MinimalEndpointVersion[] Versions => [1];

    /// <summary>
    /// Extra problem responses for this group only; the global problems (StandardProblems by default) already cover the common ones.
    /// </summary>
    public virtual int[] Problems => [];

    /// <summary>Adds conventions to every endpoint of the group, such as narrower authorization.</summary>
    public virtual void Configure(RouteGroupBuilder builder)
    {
    }
}
