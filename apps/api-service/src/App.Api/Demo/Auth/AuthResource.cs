namespace Aurore.Blueprints.ApiService.App.Api.Demo.Auth;

/// <summary>
/// Authorization variants: scopes, roles, combined requirements and pinning an authentication scheme.
/// The group requires an authenticated caller by default.
/// </summary>
internal sealed class AuthResource() : DemoResource("auth", "Authorization")
{
}
