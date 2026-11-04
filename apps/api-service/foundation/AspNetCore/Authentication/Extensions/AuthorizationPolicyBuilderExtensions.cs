using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Microsoft.AspNetCore.Authorization;

namespace Aurore.Foundation.AspNetCore.Authentication.Extensions;

public static class AuthorizationPolicyBuilderExtensions
{
    /// <summary>
    /// OAuth2 sends scopes as ONE space-separated claim ("api.read api.write"), so RequireClaim("scope", "api.read") never
    /// matches. This passes when the caller has every listed scope. Covers "scope", "scp" and the URI JwtBearer maps "scp" to.
    /// </summary>
    public static AuthorizationPolicyBuilder RequireAuroreScope(this AuthorizationPolicyBuilder builder, params string[] scopes)
    {
        return builder
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
            {
                var granted = context.User
                    .FindAll(x => x.Type is "scope" or "scp" or "http://schemas.microsoft.com/identity/claims/scope")
                    .SelectMany(x => x.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    .ToHashSet(StringComparer.Ordinal);

                return scopes.All(granted.Contains);
            });
    }

    /// <summary>
    /// Passes when the caller has ANY of the roles, wherever the provider put them: standard role claims ("role", "roles",
    /// ClaimTypes.Role) or Keycloak realm roles, which arrive as JSON inside one claim: "realm_access": { "roles": [...] }.
    /// </summary>
    public static AuthorizationPolicyBuilder RequireAuroreRole(this AuthorizationPolicyBuilder builder, params string[] roles)
    {
        return builder
            .RequireAuthenticatedUser()
            .RequireAssertion(context => GetRoles(context.User).Overlaps(roles));
    }

    /// <summary>
    /// Keycloak client roles: "resource_access": { "&lt;clientId&gt;": { "roles": [...] } }. Passes when the caller has ANY of the roles.
    /// </summary>
    public static AuthorizationPolicyBuilder RequireAuroreKeycloakClientRole(this AuthorizationPolicyBuilder builder, string clientId, params string[] roles)
    {
        return builder
            .RequireAuthenticatedUser()
            .RequireAssertion(context => context.User
                .FindAll(KeycloakClaims.ResourceAccess)
                .SelectMany(x => ReadKeycloakRoles(x.Value, clientId))
                .ToHashSet(StringComparer.Ordinal)
                .Overlaps(roles));
    }

    private static HashSet<string> GetRoles(ClaimsPrincipal user)
    {
        var roles = user
            .FindAll(x => x.Type is ClaimTypes.Role or "role" or "roles")
            .Select(x => x.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var claim in user.FindAll(KeycloakClaims.RealmAccess))
            roles.UnionWith(ReadKeycloakRoles(claim.Value, clientId: null));

        return roles;
    }

    private static string[] ReadKeycloakRoles(string json, string? clientId)
    {
        try
        {
            using var document = JsonDocument.Parse(json);

            var source = document.RootElement;

            if (clientId is not null)
            {
                if (source.TryGetProperty(clientId, out var client) is false)
                    return [];

                source = client;
            }

            if (source.TryGetProperty("roles", out var roles) is false || roles.ValueKind is not JsonValueKind.Array)
                return [];

            return [.. roles.EnumerateArray().Where(x => x.ValueKind is JsonValueKind.String).Select(x => x.GetString()!)];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
