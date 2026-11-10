using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.AspNetCore.Authentication.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aurore.Foundation.AspNetCore.Authentication.Handlers;

/// <summary>
/// The caller identity is the client name; its Roles become role claims and its Scopes one "scope" claim, so the
/// RequireRoles/RequireScopes work for API-key callers exactly as for JWT callers.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Authenticates the caller when the key header matches one of the configured clients.</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var presented = Request.Headers[Options.HeaderName].ToString();

        if (presented.HasValue() is false)
            return Task.FromResult(AuthenticateResult.NoResult());

        // Hashing gives both sides the same length, so the comparison time reveals nothing about the key.
        var presentedHash = SHA256.HashData(Encoding.UTF8.GetBytes(presented));

        foreach (var (client, settings) in Options.Clients)
        {
            var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(settings.Key));

            if (CryptographicOperations.FixedTimeEquals(presentedHash, expectedHash))
                return Task.FromResult(AuthenticateResult.Success(CreateTicket(client, settings)));
        }

        return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
    }

    /// <summary>Answers an unauthenticated request with a 401.</summary>
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = $"{ApiKeyDefaults.AuthenticationScheme} header=\"{Options.HeaderName}\"";

        return Task.CompletedTask;
    }

    private AuthenticationTicket CreateTicket(string client, ApiKeyClientOptions settings)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, client),
            new(ClaimTypes.Name, client),
            .. settings.Roles.Select(x => new Claim(ClaimTypes.Role, x)),
        ];

        if (settings.Scopes.Length > 0)
            claims.Add(new Claim("scope", string.Join(' ', settings.Scopes)));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));

        return new AuthenticationTicket(principal, Scheme.Name);
    }
}
