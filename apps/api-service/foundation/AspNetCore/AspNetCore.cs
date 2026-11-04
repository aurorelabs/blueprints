using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Asp.Versioning;
using Aurore.Foundation.Core;
using Aurore.Foundation.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Aurore.Foundation.AspNetCore;

public static class SerializationConfiguration
{
    public static IHostApplicationBuilder ConfigureAuroreSerializers(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<JsonOptions>(o =>
            JsonSerializerConfiguration.Configure(o.SerializerOptions));

        return builder;
    }
}

public static class VersioningConfiguration
{
    public static IServiceCollection AddAuroreApiVersioning(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.DefaultApiVersion = new ApiVersion(1);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}

public static class RequestContextConfiguration
{
    public static IServiceCollection AddRequestContext(this IServiceCollection services)
    {
        return services
            .AddScoped<RequestContext>();
    }

    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder builder)
    {
        return builder
            .UseMiddleware<RequestContextMiddleware>();
    }
}

public static class HttpContextExtensions
{
    public static string GetRequestHeader(this HttpContext context, string key, string? fallbackValue = null)
    {
        return context.Request.Headers.TryGetValue(key, out var headerValue) is not true
            ? fallbackValue ?? string.Empty
            : headerValue.ToString();
    }
}

public static class ApplicationHeaders
{
    public const string CorrelationId = "Correlation-Id";
    public const string IdempotencyKey = "Idempotency-Key";
    public const string TraceParent = "traceparent";
}

public sealed class RequestContextMiddleware(RequestDelegate next)
{
    private const int MaxCorrelationIdLength = 64;

    public Task InvokeAsync(HttpContext context)
    {
        var requestContext = context.RequestServices.GetRequiredService<RequestContext>();

        var correlationId = SanitizeCorrelationId(context.GetRequestHeader(ApplicationHeaders.CorrelationId));
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var spanId = Activity.Current?.SpanId.ToString();
        var traceParent = Activity.Current?.Id;
        var idempotencyKey = context.GetRequestHeader(ApplicationHeaders.IdempotencyKey);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ApplicationHeaders.CorrelationId] = correlationId;

            if (traceParent.HasValue())
                context.Response.Headers[ApplicationHeaders.TraceParent] = traceParent;

            return Task.CompletedTask;
        });

        requestContext.Update(correlationId, traceId, spanId);
        requestContext.UpdateIdempotencyKey(idempotencyKey.HasValue() ? idempotencyKey : null);

        var logger = context.RequestServices.GetRequiredService<ILogger<RequestContextMiddleware>>();

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            [OpenTelemetryProperties.Attributes.Custom.CorrelationId] = correlationId,
        });

        return next(context);
    }

    private static string SanitizeCorrelationId(string value)
    {
        if (value.Length is 0 or > MaxCorrelationIdLength)
            return Guid.NewGuid().ToString("D");

        foreach (var c in value)
        {
            if (char.IsAsciiLetterOrDigit(c) is false && c is not ('-' or '_' or '.'))
                return Guid.NewGuid().ToString("D");
        }

        return value;
    }
}

public sealed record ResponseCompressionConfigurationOptions
{
    public bool IsEnabled { get; init; } = true;
    public bool EnableForHttps { get; init; } = true;
}

public static class CompressionConfiguration
{
    public static IServiceCollection AddAuroreResponseCompression(this IServiceCollection services, ResponseCompressionConfigurationOptions? options = null)
    {
        options ??= new ResponseCompressionConfigurationOptions();

        if (options.IsEnabled is false)
            return services;

        return services
            .AddResponseCompression(o =>
            {
                // CAUTION: Can expose responses to CRIME/BREACH attacks when enabled.
                o.EnableForHttps = options.EnableForHttps;

                o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([MediaTypeNames.Application.ProblemJson]);

                // Order matters. When the client accepts both equally, the first matching provider is used.
                o.Providers.Add<BrotliCompressionProvider>();
                o.Providers.Add<GzipCompressionProvider>();
            })
            .Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
    }
}

public static class SecuritySchemes
{
    public const string JwtBearer = "Bearer";
    public const string Oidc = "OIDC";
    public const string OAuth2 = "OAuth2";
}

public static class ScalarExtensions
{
    public static string[] ConfigureScalarDocuments(this IHostApplicationBuilder builder, params int[] versions)
    {
        var documents = versions.Select(v => new ScalarDocument($"v{v}", $"Version {v}"));

        builder.Services.Configure<ScalarOptions>(x => x.AddDocuments(documents));

        return [.. documents.Select(x => x.Name)];
    }

    public static OpenApiOptions AddAppInfo(this OpenApiOptions options, string apiVersion, AppInformationOptions appInfo)
    {
        var companyName = "aurorelabs";

        return options.AddDocumentTransformer((doc, _, _) =>
        {
            doc.Info.Title = $"{appInfo.Component.ToTitleCase()} API";
            doc.Info.Description = appInfo.Description;
            doc.Info.Version = apiVersion;
            doc.Info.Contact = new()
            {
                Name = appInfo.Maintainer.Name,
                Email = $"teams.{appInfo.Maintainer.Name}@{companyName}.com",
                Url = new Uri($"https://{companyName}.com/teams/{appInfo.Maintainer.Name}")
            };

            return Task.CompletedTask;
        });
    }

    public static OpenApiOptions AddSecuritySchemeAndRequirement(this OpenApiOptions options, OpenApiSecurityScheme schema, params string[] scopes)
    {
        return options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Security ??= [];

            if (schema.Name.HasValue() is false)
                throw new ArgumentException("Security scheme must have a valid Name property.", nameof(schema));

            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes[schema.Name] = schema;

            document.Security.Add(new()
            {
                { new(schema.Name, document), [.. scopes] }
            });

            return Task.CompletedTask;
        });
    }

    public static OpenApiOptions AddJwtBearerSecurity(this OpenApiOptions options)
    {
        return options.AddSecuritySchemeAndRequirement(new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = SecuritySchemes.JwtBearer,
            Description = "JWT Authorization header using the Bearer scheme.",
            In = ParameterLocation.Header,
            Scheme = SecuritySchemes.JwtBearer.ToLower(),
            BearerFormat = "JWT"
        });
    }

    public static OpenApiOptions AddOidcSecurity(this OpenApiOptions options, IdentityProviderOptions identityProvider)
    {
        return options.AddSecuritySchemeAndRequirement(new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OpenIdConnect,
            Name = SecuritySchemes.Oidc,
            Description = "OIDC Authorization header using the Bearer scheme.",
            OpenIdConnectUrl = new Uri($"{identityProvider.AuthorityUrl}/.well-known/openid-configuration")
        });
    }

    public static OpenApiOptions AddOAuth2Security(this OpenApiOptions options, IdentityProviderOptions identityProvider)
    {
        var baseUrl = $"{identityProvider.AuthorityUrl}/protocol/openid-connect";

        return options.AddSecuritySchemeAndRequirement(new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Name = SecuritySchemes.OAuth2,
            Description = "OAuth2 Authorization Code Flow",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{baseUrl}/auth"),
                    TokenUrl = new Uri($"{baseUrl}/token"),
                    RefreshUrl = new Uri($"{baseUrl}/refresh"),
                    Scopes = identityProvider.Scopes
                }
            }
        });
    }

    public static OpenApiOptions AddCorrelationIdHeader(this OpenApiOptions options)
    {
        return options.AddDocumentTransformer((document, _, _) =>
        {
            foreach (var pathItem in document.Paths.Values)
            {
                if (pathItem.Operations is null)
                    continue;

                foreach (var operation in pathItem.Operations.Values)
                {
                    operation.Parameters ??= [];

                    if (operation.Parameters.All(p => p.Name != ApplicationHeaders.CorrelationId))
                    {
                        operation.Parameters.Add(new OpenApiParameter
                        {
                            Name = ApplicationHeaders.CorrelationId,
                            In = ParameterLocation.Header,
                            Required = false,
                            Schema = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "uuid",
                                Example = JsonValue.Create(Guid.NewGuid().ToString())
                            },
                            Description = "Correlation Id for request tracking and tracing"
                        });
                    }
                }
            }

            return Task.CompletedTask;
        });
    }
}

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddAuroreAuthentication(this IServiceCollection services, IdentityProviderOptions identityProvider)
    {
        return services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = identityProvider.AuthorityUrl;
                options.Audience = identityProvider.Audience;
                options.RequireHttpsMetadata = identityProvider.RequireHttpsMetadata;
            })
            .Services;
    }

    public static IServiceCollection AddAuroreAuthorization(this IServiceCollection services)
    {
        return services
            .AddAuthorization(options => options
                .AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy => policy
                    .RequireAuthenticatedUser()));
    }
}

public static class CorsConfiguration
{
    private const string DefaultPolicyName = "default";

    // Always allowed on every configured policy; configuration can add more headers but not remove these.
    private static readonly string[] _defaultAllowedHeaders =
    [
        HeaderNames.Authorization,
        HeaderNames.ContentType,
        ApplicationHeaders.CorrelationId,
        ApplicationHeaders.IdempotencyKey,
        ApplicationHeaders.TraceParent,
        HeaderNames.TraceState,
    ];

    // Response headers the browser lets front-end code read; configuration can add more.
    private static readonly string[] _defaultExposedHeaders =
    [
        ApplicationHeaders.CorrelationId,
        ApplicationHeaders.TraceParent,
    ];

    public static IServiceCollection AddAllowAllCorsPolicy(this IServiceCollection services)
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

            // appsettings.json holds development values; a local origin in production means the
            // production configuration (appsettings.Production.json or environment variables) is missing.
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

        // "OPTIONS" does not need to be in AllowedMethods: a preflight is validated against the method in its
        // Access-Control-Request-Method header (the actual request's method), never against OPTIONS itself.
        // If preflights fail, first check that UseCors() runs before UseAuthentication()/UseAuthorization();
        // if the problem persists, add "OPTIONS" back to AllowedMethods.
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

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddAuroreHealthChecks(
        this IServiceCollection services,
        AppInformationOptions appInfo,
        HealthChecksOptions options,
        IReadOnlyDictionary<string, HttpDependencyOptions> dependencies)
    {
        // Always registered, so the endpoints work even when there are no dependencies to check.
        var healthChecks = services.AddHealthChecks();

        foreach (var (name, dependency) in dependencies)
        {
            if (dependency.HealthCheck is not null)
                healthChecks.AddAuroreHttpDependencyCheck(appInfo, name, dependency.BaseUrl, dependency.HealthCheck);
        }

        // Named policy only: it doesn't change output caching for the rest of the application.
        services.AddOutputCache(o => o
            .AddPolicy(HealthCheckProperties.Configuration.OutputCacheKey, policy => policy
                .Expire(TimeSpan.FromSeconds(options.CacheSeconds))
                .Cache()));

        return services;
    }

    // /health/live    The process is up. Anonymous and never touches dependencies, so it is safe for frequent probes.
    // /health/ready   Critical dependencies only, status without details. Anonymous, for load balancers and orchestrators.
    // /health/status  All dependencies with details. Requires an authenticated caller since it reveals dependency names.
    //
    // HTTP status codes use the framework defaults: Healthy and Degraded return 200, Unhealthy returns 503.
    // Non-critical dependencies are registered with a Degraded failure status, so they never make the service Unhealthy.
    public static WebApplication MapAuroreHealthChecks(this WebApplication app, AppInformationOptions appInfo)
    {
        var serviceName = appInfo.Component.ToSlugCase();

        app.MapGet(HealthCheckProperties.Endpoints.Liveness, () => Results.Ok(LivenessReport.Create(serviceName, appInfo.Version)))
            .WithName("HealthCheckLiveness")
            .ExcludeFromDescription();

        app.MapHealthChecks(HealthCheckProperties.Endpoints.Readiness, CreateOptions(IsCriticalReadinessCheck, includeDependencies: false))
            .WithName("HealthCheckReadiness")
            .CacheOutput(HealthCheckProperties.Configuration.OutputCacheKey);

        app.MapHealthChecks(HealthCheckProperties.Endpoints.Status, CreateOptions(IsReadinessCheck, includeDependencies: true))
            .WithName("HealthCheckStatus")
            .CacheOutput(HealthCheckProperties.Configuration.OutputCacheKey)
            .RequireAuthorization();

        return app;
    }

    private static HealthCheckOptions CreateOptions(Func<HealthCheckRegistration, bool> predicate, bool includeDependencies)
    {
        return new HealthCheckOptions
        {
            Predicate = predicate,
            AllowCachingResponses = true,
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(
                HealthCheckReport.From(report, includeDependencies),
                context.RequestAborted),
        };
    }

    private static bool IsReadinessCheck(HealthCheckRegistration check)
    {
        return check.Tags.Contains(HealthCheckProperties.Tags.Readiness);
    }

    private static bool IsCriticalReadinessCheck(HealthCheckRegistration check)
    {
        return IsReadinessCheck(check) && check.Tags.Contains(HealthCheckProperties.Tags.Critical);
    }
}

public static class HttpDependencyHealthCheckConfiguration
{
    public static IHealthChecksBuilder AddAuroreHttpDependencyCheck(
        this IHealthChecksBuilder builder,
        AppInformationOptions appInfo,
        string name,
        string baseUrl,
        HttpDependencyHealthCheckOptions options)
    {
        var checkName = name.ToSlugCase();

        if (Uri.TryCreate(baseUrl, UriKind.Absolute, out _) is false)
            throw new InvalidOperationException($"Dependency '{checkName}' has an invalid BaseUrl '{baseUrl}'. It must be an absolute URL, e.g. 'https://orders.internal'.");

        if (options.TimeoutSeconds <= 0)
            throw new InvalidOperationException($"Dependency '{checkName}' has an invalid health check TimeoutSeconds '{options.TimeoutSeconds}'. It must be greater than zero.");

        var timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        var failureStatus = options.IsCritical ? HealthStatus.Unhealthy : HealthStatus.Degraded;

        var tags = new[]
            {
                HealthCheckProperties.Tags.Readiness,
                HealthCheckProperties.Tags.TypeHttpService,
                options.IsCritical ? HealthCheckProperties.Tags.Critical : HealthCheckProperties.Tags.NonCritical,
            }
            .Concat(options.Tags.Select(x => x.ToSlugCase()))
            .Distinct(StringComparer.Ordinal);

        builder.Services.AddHttpClient(HttpDependencyHealthCheck.GetClientName(checkName), client =>
        {
            client.BaseAddress = new Uri($"{baseUrl.TrimEnd('/')}/{options.ReadinessEndpoint.TrimStart('/')}");
            client.Timeout = timeout;
            client.DefaultRequestHeaders.Add(HealthCheckProperties.Headers.Client, appInfo.Component.ToSlugCase());
        });

        return builder.AddCheck<HttpDependencyHealthCheck>(checkName, failureStatus, tags, timeout);
    }
}

public sealed class HttpDependencyHealthCheck(ILogger<HttpDependencyHealthCheck> logger, IHttpClientFactory clientFactory) : IHealthCheck
{
    private const string ClientNamePrefix = "health-check-";

    public static string GetClientName(string checkName)
    {
        return $"{ClientNamePrefix}{checkName}";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var name = context.Registration.Name;
        var failureStatus = context.Registration.FailureStatus;

        try
        {
            var client = clientFactory.CreateClient(GetClientName(name));

            using var response = await client.GetAsync(string.Empty, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy();

            logger.LogWarning("Health check for dependency {DependencyName} returned status code {StatusCode}.", name, (int)response.StatusCode);

            return new HealthCheckResult(failureStatus, $"Dependency returned status code {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check for dependency {DependencyName} could not reach the dependency.", name);

            return new HealthCheckResult(failureStatus, "Dependency could not be reached.", ex);
        }
    }
}

public sealed record LivenessReport
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required DateTime Timestamp { get; init; }

    public static LivenessReport Create(string name, string version)
    {
        return new()
        {
            Name = name,
            Version = version,
            Timestamp = DateTime.UtcNow,
        };
    }
}

public sealed record HealthCheckReport
{
    public required HealthStatus Status { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public required DateTime Timestamp { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<DependencyReport>? Dependencies { get; init; }

    public static HealthCheckReport From(HealthReport report, bool includeDependencies)
    {
        return new()
        {
            Status = report.Status,
            TotalDuration = report.TotalDuration,
            Timestamp = DateTime.UtcNow,
            Dependencies = includeDependencies ? report.Entries.Select(DependencyReport.From).ToList() : null,
        };
    }
}

public sealed record DependencyReport
{
    public required string Name { get; init; }
    public required HealthStatus Status { get; init; }
    public string? Description { get; init; }
    public required TimeSpan Duration { get; init; }
    public required string[] Tags { get; init; }

    public static DependencyReport From(KeyValuePair<string, HealthReportEntry> entry)
    {
        return new()
        {
            Name = entry.Key,
            Status = entry.Value.Status,
            Description = entry.Value.Description,
            Duration = entry.Value.Duration,
            Tags = [.. entry.Value.Tags],
        };
    }
}
