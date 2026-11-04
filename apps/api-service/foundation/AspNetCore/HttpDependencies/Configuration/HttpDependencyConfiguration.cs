using System;
using System.Collections.Generic;
using Aurore.Foundation.AspNetCore.HttpDependencies.Handlers;
using Aurore.Foundation.AspNetCore.HttpDependencies.Services;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.Core.HttpDependencies.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aurore.Foundation.AspNetCore.HttpDependencies.Configuration;

public static class HttpDependencyConfiguration
{
    /// <summary>
    /// Registers a named HttpClient for an upstream dependency with its base address, default headers, Correlation-Id
    /// propagation and the standard resilience pipeline. Resolve it with <c>IHttpClientFactory.CreateClient(name)</c>.
    /// Chain <see cref="AddAuroreApiKey"/>, <see cref="AddAuroreClientCredentials"/> or a custom
    /// <c>AddHttpMessageHandler&lt;T&gt;()</c> to authenticate; without any of them the client is anonymous.
    /// </summary>
    public static IHttpClientBuilder AddAuroreHttpDependency(this IServiceCollection services, string name, HttpDependencyOptions dependency)
    {
        if (Uri.TryCreate(dependency.BaseUrl, UriKind.Absolute, out _) is false)
            throw new InvalidOperationException($"Dependency '{name}' has an invalid BaseUrl '{dependency.BaseUrl}'. It must be an absolute URL, e.g. 'https://orders.internal'.");

        var baseAddress = new Uri($"{dependency.BaseUrl.TrimEnd('/')}/");

        services.AddHttpContextAccessor();
        services.TryAddTransient<CorrelationIdPropagationHandler>();

        var builder = services.AddHttpClient(name, client =>
        {
            client.BaseAddress = baseAddress;

            foreach (var (header, value) in dependency.RequestHeaders)
                client.DefaultRequestHeaders.TryAddWithoutValidation(header, value);
        });

        builder.AddStandardResilienceHandler();

        if (dependency.PropagateCorrelationId)
            builder.AddHttpMessageHandler<CorrelationIdPropagationHandler>();

        return builder;
    }

    /// <summary>
    /// Same as <see cref="AddAuroreHttpDependency(IServiceCollection, string, HttpDependencyOptions)"/>, plus a typed client
    /// bound to the same name, so <typeparamref name="TClient"/> receives the configured HttpClient.
    /// </summary>
    public static IHttpClientBuilder AddAuroreHttpDependency<TClient, TImplementation>(this IServiceCollection services, string name, HttpDependencyOptions dependency)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>(name);

        return services.AddAuroreHttpDependency(name, dependency);
    }

    /// <summary>Sends a static header with every request, e.g. <c>X-Api-Key</c> or <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
    public static IHttpClientBuilder AddAuroreApiKey(this IHttpClientBuilder builder, UpstreamApiKeyOptions? options)
    {
        if (options is null || options.HeaderName.HasValue() is false || options.Value.HasValue() is false)
            throw new InvalidOperationException($"Dependency '{builder.Name}' needs an ApiKey section with HeaderName and Value.");

        return builder.AddHttpMessageHandler(() => new ApiKeyHandler(options));
    }

    /// <summary>
    /// Authenticates with the OAuth2 client-credentials grant: tokens are requested from <c>TokenUrl</c>, cached per
    /// dependency until shortly before they expire, and sent as <c>Authorization: Bearer</c>.
    /// </summary>
    public static IHttpClientBuilder AddAuroreClientCredentials(this IHttpClientBuilder builder, ClientCredentialsOptions? options)
    {
        if (options is null || Uri.TryCreate(options.TokenUrl, UriKind.Absolute, out _) is false)
            throw new InvalidOperationException($"Dependency '{builder.Name}' needs a ClientCredentials section with an absolute TokenUrl.");

        if (options.ClientId.HasValue() is false || options.ClientSecret.HasValue() is false)
            throw new InvalidOperationException($"Dependency '{builder.Name}' ClientCredentials needs both ClientId and ClientSecret.");

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.TryAddSingleton<ClientCredentialsTokenCache>();
        builder.Services.AddHttpClient(ClientCredentialsTokenCache.TokenEndpointClientName, client => client.Timeout = TimeSpan.FromSeconds(10));

        var name = builder.Name;

        return builder.AddHttpMessageHandler(provider => new ClientCredentialsHandler(provider.GetRequiredService<ClientCredentialsTokenCache>(), name, options));
    }

    /// <summary>Returns the dependency configured under <paramref name="name"/>, or throws naming the missing key.</summary>
    public static HttpDependencyOptions GetRequired(this IReadOnlyDictionary<string, HttpDependencyOptions> dependencies, string name)
    {
        return dependencies.TryGetValue(name, out var dependency)
            ? dependency
            : throw new InvalidOperationException($"Dependency '{name}' is not configured in the 'Dependencies' section.");
    }
}
