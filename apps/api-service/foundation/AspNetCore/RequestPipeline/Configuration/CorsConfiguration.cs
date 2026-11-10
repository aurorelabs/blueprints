using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using Aurore.Foundation.AspNetCore.Constants;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;

/// <summary>Registers CORS policies.</summary>
public static class CorsConfiguration
{
    private const string DefaultPolicyName = "default";

    private static readonly string[] _defaultAllowedHeaders =
    [
        HeaderNames.Authorization,
        HeaderNames.ContentType,
        CustomHeaderNames.CorrelationId,
        CustomHeaderNames.IdempotencyKey,
        HeaderNames.TraceParent,
        HeaderNames.TraceState,
    ];

    private static readonly string[] _defaultExposedHeaders =
    [
        CustomHeaderNames.CorrelationId,
        HeaderNames.TraceParent,
    ];

    /// <summary>
    /// CORS is optional: without policies nothing is registered and browsers are limited to same-origin calls.
    /// Configure policies only when a browser app calls this service directly from another origin.
    /// </summary>
    internal static IHostApplicationBuilder AddAuroreCorsPolicies(this IHostApplicationBuilder builder, IReadOnlyDictionary<string, CorsPolicyOptions> policies)
    {
        if (policies.Count == 0)
        {
            return builder;
        }

        ValidatePolicies(policies, builder.Environment.IsProduction());

        builder.Services.AddCors(options =>
        {
            foreach (var policy in policies)
            {
                if (policy.Key.IsEqualTo(DefaultPolicyName))
                    options.AddDefaultPolicy(BuildPolicy(policy.Value));
                else
                    options.AddPolicy(policy.Key, BuildPolicy(policy.Value));
            }
        });

        return builder;
    }

    /// <summary>
    /// Pairs with AddAuroreCorsPolicies: UseCors() fails at startup when CORS services were never registered.
    /// </summary>
    internal static IApplicationBuilder UseAuroreCors(this IApplicationBuilder builder, IReadOnlyDictionary<string, CorsPolicyOptions> policies)
    {
        return policies.Count > 0 ? builder.UseCors() : builder;
    }

    private static void ValidatePolicies(IReadOnlyDictionary<string, CorsPolicyOptions> policies, bool isProduction)
    {
        if (policies.Keys.Count(x => x.IsEqualTo(DefaultPolicyName)) > 1)
            throw new InvalidOperationException($"Only one '{DefaultPolicyName}' CORS policy is allowed.");

        foreach (var (name, policy) in policies)
        {
            var allowsAnyOrigin = policy.AllowedOrigins.Contains("*");

            if (allowsAnyOrigin && policy.AllowCredentials)
                throw new InvalidOperationException($"CORS policy '{name}' cannot allow credentials with a wildcard ('*') origin.");

            if (allowsAnyOrigin && isProduction)
                throw new InvalidOperationException($"CORS policy '{name}' cannot allow any origin ('*') in production.");

            var invalidOrigin = policy.AllowedOrigins.FirstOrDefault(x =>
                x != "*" && (!Uri.TryCreate(x, UriKind.Absolute, out var uri) || uri.PathAndQuery != "/" || x.EndsWith('/')));

            if (invalidOrigin is not null)
                throw new InvalidOperationException($"CORS policy '{name}' has an invalid origin '{invalidOrigin}'. Origins must be scheme + host (+ port) with no path or trailing slash, e.g. 'https://app.example.com'.");

            var localOrigin = isProduction ? policy.AllowedOrigins.FirstOrDefault(IsLocalOrigin) : null;

            if (localOrigin is not null)
                throw new InvalidOperationException($"CORS policy '{name}' allows the local origin '{localOrigin}' in production. Configure the production origins in appsettings.Production.json or environment variables.");
        }
    }

    private static bool IsLocalOrigin(string origin)
    {
        return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.IsLoopback || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase));
    }

    private static Action<CorsPolicyBuilder> BuildPolicy(CorsPolicyOptions policy)
    {
        var allowedHeaders = MergeHeaders(_defaultAllowedHeaders, policy.AllowedHeaders);
        var exposedHeaders = MergeHeaders(_defaultExposedHeaders, policy.ExposedHeaders);

        // OPTIONS is not needed in AllowedMethods: preflights are checked against Access-Control-Request-Method.
        // If preflights fail, make sure UseCors() runs before authentication; otherwise add OPTIONS back.
        return cors =>
        {
            cors
                .WithOrigins(policy.AllowedOrigins)
                .WithMethods(policy.AllowedMethods)
                .WithHeaders(allowedHeaders)
                .WithExposedHeaders(exposedHeaders)
                .SetPreflightMaxAge(TimeSpan.FromSeconds(policy.PreflightMaxAgeSeconds));

            if (policy.AllowCredentials)
                cors.AllowCredentials();
        };
    }

    private static string[] MergeHeaders(string[] defaults, string[] configured)
    {
        return [.. defaults.Concat(configured).Distinct(StringComparer.OrdinalIgnoreCase)];
    }
}
