using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.AspNetCore.Authentication.Handlers;
using Aurore.Foundation.AspNetCore.Authentication.Models;
using Aurore.Foundation.AspNetCore.Authentication.Services;
using Aurore.Foundation.Core.Authentication.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.AspNetCore.Authentication.Configuration;

/// <summary>Registers authentication and authorization: JWT Bearer, and API keys when clients are configured.</summary>
public static class AuthenticationConfiguration
{
    /// <summary>
    /// JWT Bearer (Authorization: Bearer &lt;token&gt;) always; API keys too when at least one client is configured.
    /// With both, every RequireAuthorization()/RequireScopes()/RequireRoles() accepts either kind of caller. Roles from the
    /// token are normalized to standard role claims according to IdentityProviderOptions.Provider.
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
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(identityProvider.ClockSkewSeconds);
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                            RoleClaimsMapper.Map(identity, identityProvider.Provider, identityProvider.Audience);

                        return Task.CompletedTask;
                    },
                };
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

    /// <summary>
    /// Adds the API key scheme, reading the key from the configured header and matching it against the configured
    /// clients.
    /// </summary>
    public static AuthenticationBuilder AddAuroreApiKeyAuthentication(this AuthenticationBuilder builder, ApiKeysOptions apiKeys)
    {
        return builder.AddScheme<ApiKeyAuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.AuthenticationScheme, options =>
        {
            options.HeaderName = apiKeys.HeaderName;
            options.Clients = apiKeys.Clients;
        });
    }
}
