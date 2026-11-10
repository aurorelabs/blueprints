namespace Aurore.Blueprints.ApiService.App.Api.Demo.Diagnostics;

/// <summary>
/// Correlation ids, error responses and an endpoint that opts in to authorization inside an anonymous group.
/// </summary>
internal sealed class DiagnosticsResource() : DemoResource("diagnostics", "Diagnostics")
{
    public override bool RequireAuthorization => false;
}
