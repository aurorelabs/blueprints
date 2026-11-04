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

    private static IServiceCollection AddAllowAllCorsPolicy(this IServiceCollection services)
    {
        return services
            .AddCors(options => options
                .AddDefaultPolicy(builder => builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin()
                    .WithExposedHeaders(_defaultExposedHeaders)));
    }

    public static IHostApplicationBuilder AddAuroreCorsPolicies(this IHostApplicationBuilder builder, IReadOnlyDictionary<string, CorsPolicyOptions> policies)
    {
        var isProduction = builder.Environment.IsProduction();

        if (policies.Count == 0)
        {
            if (isProduction)
                throw new InvalidOperationException("No CORS policies are configured. Production requires at least one explicit CORS policy in the 'CorsPolicies' configuration section.");

            builder.Services.AddAllowAllCorsPolicy();

            return builder;
        }

        ValidatePolicies(policies, isProduction);

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
