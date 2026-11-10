using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Application.Models;

/// <summary>
/// Settings for the interactive API documentation, which only runs outside production.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record ApiDocsOptions
{
    /// <summary>
    /// Client id of a public identity provider client used to sign in from the documentation with the authorization code
    /// flow and PKCE. A public client has no secret, so the id is not sensitive. Empty leaves the sign-in unconfigured
    /// (a token can still be pasted by hand).
    /// </summary>
    public string OAuthClientId { get; init; } = string.Empty;
}
