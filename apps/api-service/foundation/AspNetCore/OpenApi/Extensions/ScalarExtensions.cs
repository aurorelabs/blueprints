using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Asp.Versioning;
using Aurore.Foundation.AspNetCore.Configuration;
using Aurore.Foundation.AspNetCore.Constants;
using Aurore.Foundation.AspNetCore.MinimalApis;
using Aurore.Foundation.AspNetCore.OpenApi.Constants;
using Aurore.Foundation.AspNetCore.RequestPipeline.Filters;
using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Aurore.Foundation.AspNetCore.OpenApi.Extensions;

/// <summary>Configures the OpenAPI documents and the Scalar reference UI.</summary>
public static class ScalarExtensions
{
    /// <summary>
    /// OpenAPI document name for a version (v1, v2...); built with the same format the API explorer uses for its groups,
    /// so a document always matches the endpoints of its version.
    /// </summary>
    public static string DocumentName(int version)
    {
        return new ApiVersion(version).ToString(VersioningConfiguration.GroupNameFormat);
    }

    /// <summary>
    /// Registers one Scalar document per version, in the order given (the first one is the default).
    /// </summary>
    public static IHostApplicationBuilder ConfigureAuroreScalarDocuments(this IHostApplicationBuilder builder, IEnumerable<MinimalEndpointVersion> versions)
    {
        var documents = versions
            .Select(v => new ScalarDocument(DocumentName(v.Number), v switch
            {
                { IsDeprecated: true } => $"Version {v.Number} (deprecated)",
                _ => $"Version {v.Number}",
            }))
            .ToArray();

        builder.Services.Configure<ScalarOptions>(x => x.AddDocuments(documents));

        return builder;
    }

    /// <summary>
    /// Pre-configures the "Authorize" button for the OAuth2 authorization code flow with PKCE, so signing in from the
    /// documentation redirects to the identity provider without pasting anything. Does nothing without a client id.
    /// </summary>
    public static ScalarOptions WithAuroreAuthentication(this ScalarOptions options, IdentityProviderOptions identityProvider, ApiDocsOptions apiDocs)
    {
        if (apiDocs.OAuthClientId.HasValue() is false)
            return options;

        return options
            .AddPreferredSecuritySchemes([SecuritySchemes.OAuth2])
            .AddAuthorizationCodeFlow(SecuritySchemes.OAuth2, flow => flow
                .WithClientId(apiDocs.OAuthClientId)
                .WithPkce(Pkce.Sha256)
                .WithSelectedScopes(identityProvider.Scopes.Keys));
    }

    /// <summary>Fills the document's title, version, description and contact from the application information.</summary>
    public static OpenApiOptions AddAuroreDocumentInfo(this OpenApiOptions options, MinimalEndpointVersion version, AppInformationOptions appInfo)
    {
        var notice = version switch
        {
            { IsDeprecated: true, Sunset: { } sunset } => $"**Deprecated.** This version stops being served on {sunset:yyyy-MM-dd}.",
            { IsDeprecated: true } => "**Deprecated.**",
            _ => null,
        };

        var description = string.Join("\n\n", new[] { appInfo.Description, version.Description, notice }.Where(x => string.IsNullOrWhiteSpace(x) is false));

        return options.AddDocumentTransformer((doc, _, _) =>
        {
            doc.Info.Title = $"{appInfo.Component.ToTitleCase()} API";
            doc.Info.Description = description;
            doc.Info.Version = $"{version.Number}.0";
            doc.Info.Contact = new()
            {
                Name = appInfo.Maintainer.Name,
                Email = appInfo.Maintainer.Email,
                Url = appInfo.Maintainer.Url is { Length: > 0 } url ? new Uri(url) : null
            };

            return Task.CompletedTask;
        });
    }

    /// <summary>Adds a security scheme to the document and requires it on every operation.</summary>
    public static OpenApiOptions AddAuroreSecuritySchemeAndRequirement(this OpenApiOptions options, OpenApiSecurityScheme schema, params string[] scopes)
    {
        if (schema.Name.HasValue() is false)
            throw new ArgumentException("Security scheme must have a valid Name property.", nameof(schema));

        return options
            .AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes[schema.Name] = schema;

                return Task.CompletedTask;
            })
            .AddOperationTransformer((operation, context, _) =>
            {
                // Group-level and endpoint-level RequireAuthorization()/AllowAnonymous() both end up in the endpoint metadata.
                if (RequiresAuthorization(context.Description) is false)
                    return Task.CompletedTask;

                operation.Security ??= [];

                operation.Security.Add(new()
                {
                    { new(schema.Name, context.Document), [.. scopes] }
                });

                return Task.CompletedTask;
            });
    }

    private static bool RequiresAuthorization(ApiDescription description)
    {
        var metadata = description.ActionDescriptor.EndpointMetadata;

        return metadata.OfType<IAuthorizeData>().Any() && metadata.OfType<IAllowAnonymous>().Any() is false;
    }

    /// <summary>Documents bearer token authentication.</summary>
    public static OpenApiOptions AddAuroreJwtBearerSecurity(this OpenApiOptions options)
    {
        return options.AddAuroreSecuritySchemeAndRequirement(new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = SecuritySchemes.JwtBearer,
            Description = "JWT Authorization header using the Bearer scheme.",
            In = ParameterLocation.Header,
            Scheme = SecuritySchemes.JwtBearer.ToLower(),
            BearerFormat = "JWT"
        });
    }

    /// <summary>
    /// For an apiKey scheme OpenAPI uses Name as the header name, so the scheme is listed under the header name too.
    /// </summary>
    public static OpenApiOptions AddAuroreApiKeySecurity(this OpenApiOptions options, ApiKeysOptions apiKeys)
    {
        if (apiKeys.Clients.Count == 0)
            return options;

        return options.AddAuroreSecuritySchemeAndRequirement(new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = apiKeys.HeaderName,
            Description = $"API key in the {apiKeys.HeaderName} header.",
            In = ParameterLocation.Header,
        });
    }

    /// <summary>Documents OpenID Connect authentication against the configured identity provider.</summary>
    public static OpenApiOptions AddAuroreOidcSecurity(this OpenApiOptions options, IdentityProviderOptions identityProvider)
    {
        return options.AddAuroreSecuritySchemeAndRequirement(new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OpenIdConnect,
            Name = SecuritySchemes.Oidc,
            Description = "OIDC Authorization header using the Bearer scheme.",
            OpenIdConnectUrl = new Uri($"{identityProvider.AuthorityUrl.TrimEnd('/')}/.well-known/openid-configuration")
        });
    }

    /// <summary>Documents the OAuth2 authorization code flow against the configured identity provider.</summary>
    public static OpenApiOptions AddAuroreOAuth2Security(this OpenApiOptions options, IdentityProviderOptions identityProvider)
    {
        var authority = identityProvider.AuthorityUrl.TrimEnd('/');
        var tokenUrl = new Uri($"{authority}/{identityProvider.TokenPath.TrimStart('/')}");

        return options.AddAuroreSecuritySchemeAndRequirement(new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Name = SecuritySchemes.OAuth2,
            Description = "OAuth2 Authorization Code Flow",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{authority}/{identityProvider.AuthorizationPath.TrimStart('/')}"),
                    TokenUrl = tokenUrl,

                    RefreshUrl = tokenUrl,
                    Scopes = identityProvider.Scopes
                }
            }
        });
    }

    /// <summary>
    /// Writes the names of the query parameters in camel case ("pageSize", not "PageSize"), the same rule the JSON of the
    /// bodies follows. The generator uses the C# property names for them, which the JSON naming policy never touches.
    /// Binding ignores case, so this only changes what the document says. Headers and route values are left as they are.
    /// </summary>
    public static OpenApiOptions AddAuroreCamelCaseQueryParameters(this OpenApiOptions options)
    {
        return options.AddOperationTransformer((operation, _, _) =>
        {
            foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
            {
                if (parameter.In == ParameterLocation.Query && parameter.Name.HasValue())
                    parameter.Name = parameter.Name.ToCamelCase();
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Documents the Idempotency-Key request header on the operations that use it, those marked with WithIdempotency, and
    /// only there. It is required when the endpoint was marked required: true, optional otherwise.
    /// </summary>
    public static OpenApiOptions AddAuroreIdempotencyKeyHeader(this OpenApiOptions options)
    {
        return options.AddOperationTransformer((operation, context, _) =>
        {
            var idempotency = context.Description.ActionDescriptor.EndpointMetadata.OfType<IdempotencyMetadata>().FirstOrDefault();

            if (idempotency is null)
                return Task.CompletedTask;

            operation.Parameters ??= [];

            if (operation.Parameters.All(p => p.Name != CustomHeaderNames.IdempotencyKey))
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = CustomHeaderNames.IdempotencyKey,
                    In = ParameterLocation.Header,
                    Required = idempotency.Required,
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Pattern = RequestContextMiddleware.IdempotencyKeyPattern,
                        MinLength = RequestContextMiddleware.MinIdempotencyKeyLength,
                        MaxLength = RequestContextMiddleware.MaxIdempotencyKeyLength,
                        Example = JsonValue.Create(Guid.NewGuid().ToString())
                    },
                    Description = $"Idempotency Key for request idempotence. Letters, digits, '.', '_' and '-', {RequestContextMiddleware.MinIdempotencyKeyLength} to {RequestContextMiddleware.MaxIdempotencyKeyLength} characters."
                });
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>Documents the optional Correlation-Id request header on every operation.</summary>
    public static OpenApiOptions AddAuroreCorrelationIdHeader(this OpenApiOptions options)
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

                    if (operation.Parameters.All(p => p.Name != CustomHeaderNames.CorrelationId))
                    {
                        operation.Parameters.Add(new OpenApiParameter
                        {
                            Name = CustomHeaderNames.CorrelationId,
                            In = ParameterLocation.Header,
                            Required = false,
                            Schema = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Pattern = RequestContextMiddleware.CorrelationIdPattern,
                                MinLength = RequestContextMiddleware.MinCorrelationIdLength,
                                MaxLength = RequestContextMiddleware.MaxCorrelationIdLength,
                                Example = JsonValue.Create(Guid.NewGuid().ToString())
                            },
                            Description = $"Correlation Id for request tracking and tracing. Letters, digits, '.', '_' and '-', {RequestContextMiddleware.MinCorrelationIdLength} to {RequestContextMiddleware.MaxCorrelationIdLength} characters; a new one is generated when missing or invalid."
                        });
                    }
                }
            }

            return Task.CompletedTask;
        });
    }
}
