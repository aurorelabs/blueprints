using System;
using System.Linq;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.AspNetCore.Authentication.Handlers;
using Aurore.Foundation.AspNetCore.Authentication.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.AspNetCore.Authentication.Configuration;

public static class AuthenticationConfiguration
{
    /// <summary>
    /// JWT Bearer (Authorization: Bearer &lt;token&gt;) always; API keys too when at least one client is configured.
    /// With both, every RequireAuthorization()/RequireAuroreScope()/RequireAuroreRole() accepts either kind of caller.
    /// Pin one with policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme or ApiKeyDefaults.AuthenticationScheme).
    /// </summary>
    public static IServiceCollection AddAuroreAuthConfiguration(this IServiceCollection services, IdentityProviderOptions identityProvider, ApiKeysOptions apiKeys)
    {
        var useApiKeys = apiKeys.Clients.Count > 0;
        var defaultScheme = useApiKeys ? AuroreAuthenticationDefaults.BearerOrApiKeyScheme : JwtBearerDefaults.AuthenticationScheme;

        var builder = services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = defaultScheme;
                options.DefaultChallengeScheme = defaultScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = identityProvider.AuthorityUrl;
                options.Audience = identityProvider.Audience;
                options.RequireHttpsMetadata = identityProvider.RequireHttpsMetadata;
            });

        services.AddAuthorization();

        if (useApiKeys is false)
            return services;

        builder
            .AddAuroreApiKeyAuthentication(apiKeys)
            .AddPolicyScheme(AuroreAuthenticationDefaults.BearerOrApiKeyScheme, "Bearer token or API key", options =>
            {
                options.ForwardDefaultSelector = context => context.Request.Headers.ContainsKey(apiKeys.HeaderName)
                    ? ApiKeyDefaults.AuthenticationScheme
                    : JwtBearerDefaults.AuthenticationScheme;
            });

        return services;
    }

    public static AuthenticationBuilder AddAuroreApiKeyAuthentication(this AuthenticationBuilder builder, ApiKeysOptions apiKeys)
    {
        if (apiKeys.HeaderName.HasValue() is false)
            throw new InvalidOperationException("'ApiKeys:HeaderName' must not be empty.");

        foreach (var (client, settings) in apiKeys.Clients)
        {
            if (settings.Key.HasValue() is false)
                throw new InvalidOperationException($"API key client '{client}' has an empty Key.");
        }

        var duplicate = apiKeys.Clients.GroupBy(x => x.Value.Key, StringComparer.Ordinal).FirstOrDefault(x => x.Count() > 1);

        if (duplicate is not null)
            throw new InvalidOperationException($"API key clients {string.Join(", ", duplicate.Select(x => $"'{x.Key}'"))} share the same Key.");

        return builder.AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.AuthenticationScheme, options =>
        {
            options.HeaderName = apiKeys.HeaderName;
            options.Clients = apiKeys.Clients;
        });
    }
}
