using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.Core.Authentication.Models;

namespace Aurore.Foundation.AspNetCore.Authentication.Services;

/// <summary>
/// Turns the roles of a validated token into standard role claims (ClaimTypes.Role), wherever the provider put them, so
/// the rest of the application never knows which provider issued the token.
/// </summary>
internal static class RoleClaimsMapper
{
    /// <summary>Adds the token's roles to the identity as role claims, skipping any it already has.</summary>
    internal static void Map(ClaimsIdentity identity, IdentityProviderKind provider, string audience)
    {
        var known = identity.FindAll(ClaimTypes.Role).Select(x => x.Value).ToHashSet(StringComparer.Ordinal);

        // Read everything before adding: the claims cannot be added while they are still being enumerated.
        var found = ReadRoles(identity, provider, audience).ToList();

        foreach (var role in found)
        {
            if (known.Add(role))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }
    }

    private static IEnumerable<string> ReadRoles(ClaimsIdentity identity, IdentityProviderKind provider, string audience)
    {
        if (provider is IdentityProviderKind.Keycloak)
        {
            foreach (var claim in identity.FindAll(KeycloakClaims.RealmAccess))
            {
                foreach (var role in ReadJsonRoles(claim.Value, client: null))
                    yield return role;
            }

            foreach (var claim in identity.FindAll(KeycloakClaims.ResourceAccess))
            {
                foreach (var role in ReadJsonRoles(claim.Value, audience))
                    yield return role;
            }

            yield break;
        }

        foreach (var claim in identity.FindAll(x => x.Type is "role" or "roles"))
            yield return claim.Value;
    }

    private static string[] ReadJsonRoles(string json, string? client)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var source = document.RootElement;

            if (client is not null)
            {
                if (source.ValueKind is not JsonValueKind.Object || source.TryGetProperty(client, out var found) is false)
                    return [];

                source = found;
            }

            if (source.ValueKind is not JsonValueKind.Object || source.TryGetProperty("roles", out var roles) is false || roles.ValueKind is not JsonValueKind.Array)
                return [];

            return [.. roles.EnumerateArray().Where(x => x.ValueKind is JsonValueKind.String).Select(x => x.GetString()!)];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
