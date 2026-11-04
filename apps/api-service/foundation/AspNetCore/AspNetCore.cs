using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.RateLimiting;
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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

    public static IServiceCollection AddAllowAllCorsPolicy(this IServiceCollection services)
    {
        return services
            .AddCors(options => options
                .AddDefaultPolicy(builder => builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin()));
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
        }
    }

    private static Action<CorsPolicyBuilder> BuildPolicy(CorsPolicyOptions policy)
    {
        return cors =>
        {
            cors
                .WithOrigins(policy.AllowedOrigins)
                .WithMethods(policy.AllowedMethods)
                .WithHeaders(policy.AllowedHeaders)
                .WithExposedHeaders(policy.ExposedHeaders)
                .SetPreflightMaxAge(TimeSpan.FromSeconds(policy.PreflightMaxAgeSeconds));

            if (policy.AllowCredentials)
                cors.AllowCredentials();
        };
    }
}



public static class HealthCheckConfiguration
{
    private static readonly Dictionary<HealthStatus, int> _resultStatusCodes = new()
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    };

    public static IServiceCollection AddHealthCheckRateLimiter(this IServiceCollection services, int permitLimit, int limitSeconds)
    {
        return services
            .AddRateLimiter(o =>
            {
                o.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(new { Message = "AI: FIX THIS MESSAGE" }, cancellationToken);
                };

                o.AddFixedWindowLimiter(HealthCheckProperties.Configuration.RateLimitingKey, limiter =>
                {
                    limiter.PermitLimit = permitLimit;
                    limiter.Window = TimeSpan.FromSeconds(limitSeconds);
                    limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiter.QueueLimit = 0;
                });
            });
    }

    public static IServiceCollection AddHealthCheckOutputCache(this IServiceCollection services, int expirationSeconds)
    {
        return services
            .AddOutputCache(o => o
                .AddPolicy(HealthCheckProperties.Configuration.OutputCacheKey, policy =>
                {
                    policy.Expire(TimeSpan.FromSeconds(expirationSeconds));
                    policy.SetVaryByHeader(HealthCheckProperties.Headers.AuthorizationKey);
                    policy.Cache();
                }));
    }

    public static WebApplication MapHealthCheckEndpoints(this WebApplication app, HealthCheckEndpointOptions options)
    {
        return app
            .MapLivenessEndpoint(options.ServiceName, options.Version)
            .MapReadinessEndpoint(HealthCheckProperties.Endpoints.Readiness, options.Token)
            .MapReadinessEndpoint(HealthCheckProperties.Endpoints.Status, options.Token);
    }

    private static WebApplication MapLivenessEndpoint(this WebApplication app, string name, string version)
    {
        app
            .MapGet(HealthCheckProperties.Endpoints.Liveness, () => Results.Ok(new
            {
                version,
                Name = name.ToSlugCase(),
                Timestamp = DateTime.UtcNow
            }))
            .ExcludeFromDescription()
            .RequireRateLimiting(HealthCheckProperties.Configuration.RateLimitingKey);

        return app;
    }

    private static WebApplication MapReadinessEndpoint(this WebApplication app, string path, string expectedKey)
    {
        var criticalOnly = path.IsEqualTo(HealthCheckProperties.Endpoints.Readiness);

        var options = new HealthCheckOptions
        {
            Predicate = check => ContainsReadinessTags(check, criticalOnly),
            AllowCachingResponses = true,
            ResultStatusCodes = _resultStatusCodes,
            ResponseWriter = criticalOnly
                ? SimplifiedHealthCheckReadinessReport.WriteResponseAsync
                : HealthCheckReadinessReport.WriteResponseAsync
        };

        var name = $"HealthCheck{path.Split('/').Last().ToSentenceCase()}";

        app.MapHealthChecks(path, options)
            .WithName(name)
            .WithSummary(name.ToSentenceCase())
            .CacheOutput(HealthCheckProperties.Configuration.OutputCacheKey)
            .RequireRateLimiting(HealthCheckProperties.Configuration.RateLimitingKey)
            .AddEndpointFilter(async (context, next) =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(HealthCheckConfiguration));

                var hasKey = context.HttpContext.Request.Headers.TryGetValue(HealthCheckProperties.Headers.AuthorizationKey, out var key);

                if (hasKey && key.ToString().IsEqualTo(expectedKey))
                    return await next(context);

                logger.LogWarning("Unauthorized access attempt to readiness endpoint");

                return Results.Unauthorized();
            });

        return app;
    }

    private static bool ContainsReadinessTags(HealthCheckRegistration check, bool onlyCritical)
    {
        var ready = HealthCheckProperties.Tags.Readiness;

        return onlyCritical
            ? check.Tags.Contains(ready) && check.Tags.Contains(HealthCheckProperties.Tags.Critical)
            : check.Tags.Contains(ready);
    }
}

public static class HttpServiceHealthCheckConfiguration
{
    public static IServiceCollection AddHttpServiceHealthCheck(
        this IServiceCollection services, string name, HttpServiceDependencyOptions options)
    {
        var opts = new
        {
            Name = name.ToSlugCase(),
            options.BaseUrl,
            ReadinessEndpoint = (options.HealthCheck!.ReadinessEndpoint ?? HealthCheckProperties.Endpoints.Readiness).TrimStart('/'),
            Timeout = TimeSpan.FromSeconds(options.HealthCheck!.TimeoutSeconds),
            FailureStatus = options.HealthCheck!.IsCritical ? HealthStatus.Unhealthy : HealthStatus.Degraded,
            Tags = new List<string> {
                HealthCheckProperties.Tags.Readiness,
                HealthCheckProperties.Tags.TypeHttpService,
                options.HealthCheck!.IsCritical ? HealthCheckProperties.Tags.Critical : HealthCheckProperties.Tags.NonCritical
            }.Concat(options.HealthCheck!.Tags.Select(x => x.ToSlugCase())),
        };

        services.AddHttpClient($"hc-{opts.Name}", client =>
        {
            client.DefaultRequestHeaders.Add(HealthCheckProperties.Headers.Client, opts.Name);
            client.BaseAddress = new Uri($"{options.BaseUrl}/{opts.ReadinessEndpoint}");
            client.Timeout = opts.Timeout;
        });

        services
            .AddHealthChecks()
            .AddCheck<HttpServiceHealthCheck>(name, opts.FailureStatus, opts.Tags, opts.Timeout);

        return services;
    }
}

public sealed class HttpServiceHealthCheck(ILogger<HttpServiceHealthCheck> logger, IHttpClientFactory clientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = clientFactory.CreateClient($"hc-{context.Registration.Name}");

            var response = await httpClient.GetAsync(string.Empty, cancellationToken).ConfigureAwait(false);

            return response.IsSuccessStatusCode ? HealthCheckResult.Healthy() : Failure();
        }
        catch (Exception ex)
        {
            return Failure(ex);
        }

        HealthCheckResult Failure(Exception? ex = null)
        {
            logger.LogError(ex, "Error while trying to reach service {ServiceName}.", context.Registration.Name);

            return new HealthCheckResult(context.Registration.FailureStatus, "An upstream service returned an invalid response.", ex);
        }
    }
}

public record SimplifiedHealthCheckReadinessReport
{
    public required HealthStatus Status { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public static DateTime Timestamp => DateTime.UtcNow;

    public static async Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        var response = new SimplifiedHealthCheckReadinessReport
        {
            Status = GetStatus(report),
            TotalDuration = report.TotalDuration
        };

        await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }

    protected static HealthStatus GetStatus(HealthReport report)
    {
        var status = report.Status;

        if (status == HealthStatus.Unhealthy)
            status = HasCriticalUnhealthy() ? HealthStatus.Unhealthy : HealthStatus.Degraded;

        return status;

        bool HasCriticalUnhealthy()
        {
            return report.Entries.Any(x =>
                x.Value.Tags.Contains(HealthCheckProperties.Tags.Critical) &&
                x.Value.Status is not HealthStatus.Healthy);
        }
    }
}

public sealed record HealthCheckReadinessReport : SimplifiedHealthCheckReadinessReport
{
    public IEnumerable<DependencyReport> Dependencies { get; init; } = [];

    public static new async Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        var response = new HealthCheckReadinessReport
        {
            Status = GetStatus(report),
            TotalDuration = report.TotalDuration,
            Dependencies = [.. report.Entries.Select(DependencyReport.From)]
        };

        await context.Response.WriteAsJsonAsync(response, context.RequestAborted);
    }
}
public sealed record DependencyReport
{
    public required string Name { get; init; }
    public required HealthStatus Status { get; init; }
    public required TimeSpan Duration { get; init; }
    public required string[] Tags { get; init; }

    public static DependencyReport From(KeyValuePair<string, HealthReportEntry> entry)
    {
        return new()
        {
            Name = entry.Key,
            Status = entry.Value.Status,
            Duration = entry.Value.Duration,
            Tags = [.. entry.Value.Tags]
        };
    }
}
