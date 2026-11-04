using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Constants;
using Aurore.Foundation.AspNetCore.OpenApi.Constants;
using Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;
using Aurore.Foundation.Core.Application.Models;
using Aurore.Foundation.Core.Authentication.Models;
using Aurore.Foundation.Core.Extensions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Aurore.Foundation.AspNetCore.OpenApi.Extensions;

public static class ScalarExtensions
{
    public static string[] ConfigureAuroreScalarDocuments(this IHostApplicationBuilder builder, params int[] versions)
    {
        var documents = versions.Select(v => new ScalarDocument($"v{v}", $"Version {v}"));

        builder.Services.Configure<ScalarOptions>(x => x.AddDocuments(documents));

        return [.. documents.Select(x => x.Name)];
    }

    public static OpenApiOptions AddAuroreDocumentInfo(this OpenApiOptions options, string apiVersion, AppInformationOptions appInfo)
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
                Email = appInfo.Maintainer.Email,
                Url = new Uri($"https://{companyName}.com/teams/{appInfo.Maintainer.Name}")
            };

            return Task.CompletedTask;
        });
    }

    public static OpenApiOptions AddAuroreSecuritySchemeAndRequirement(this OpenApiOptions options, OpenApiSecurityScheme schema, params string[] scopes)
    {
        if (schema.Name.HasValue() is false)
            throw new ArgumentException("Security scheme must have a valid Name property.", nameof(schema));

        return options.AddDocumentTransformer((document, _, _) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Security ??= [];

            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

            document.Components.SecuritySchemes[schema.Name] = schema;

            document.Security.Add(new()
            {
                { new(schema.Name, document), [.. scopes] }
            });

            return Task.CompletedTask;
        });
    }

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
                                MaxLength = RequestContextMiddleware.MaxCorrelationIdLength,
                                Example = JsonValue.Create(Guid.NewGuid().ToString())
                            },
                            Description = "Correlation Id for request tracking and tracing. Letters, digits, '.', '_' and '-', up to 64 characters; a new one is generated when missing or invalid."
                        });
                    }
                }
            }

            return Task.CompletedTask;
        });
    }
}
