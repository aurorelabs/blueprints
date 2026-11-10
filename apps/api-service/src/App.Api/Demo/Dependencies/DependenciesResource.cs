namespace Aurore.Blueprints.ApiService.App.Api.Demo.Dependencies;

/// <summary>
/// Calling upstream services: a typed client and a named client from IHttpClientFactory.
/// </summary>
internal sealed class DependenciesResource() : DemoResource("dependencies", "Upstream dependencies")
{
    public override bool RequireAuthorization => false;
}
