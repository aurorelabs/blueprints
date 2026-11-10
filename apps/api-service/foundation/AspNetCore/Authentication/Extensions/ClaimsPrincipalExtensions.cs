using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Aurore.Foundation.AspNetCore.Authentication.Extensions;

/// <summary>Reads the roles and scopes of the caller, whichever provider or API key they came from.</summary>
public static class ClaimsPrincipalExtensions
{
    private static readonly string[] _scopeClaimTypes = ["scope", "scp", "http://schemas.microsoft.com/identity/claims/scope"];

    /// <summary>The roles of the caller.</summary>
    public static IReadOnlySet<string> GetRoles(this ClaimsPrincipal user)
    {
        return user.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// The scopes of the caller. OAuth2 sends them as one space-separated claim ("api.read api.write"), which this splits.
    /// Covers "scope", "scp" and the URI JwtBearer maps "scp" to.
    /// </summary>
    public static IReadOnlySet<string> GetScopes(this ClaimsPrincipal user)
    {
        return user
            .FindAll(x => _scopeClaimTypes.Contains(x.Type))
            .SelectMany(x => x.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Whether the caller has the role.</summary>
    public static bool HasRole(this ClaimsPrincipal user, string role)
    {
        return user.IsInRole(role);
    }

    /// <summary>Whether the caller has the scope.</summary>
    public static bool HasScope(this ClaimsPrincipal user, string scope)
    {
        return user.GetScopes().Contains(scope);
    }
}
