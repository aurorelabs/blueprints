using System;
using Aurore.Blueprints.ApiService.App.Api.Files;
using Aurore.Foundation.AspNetCore;
using Aurore.Foundation.Core;
using Aurore.Foundation.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder
        .ConfigureAuroreSerializers()
        .AddAppSettings(out var appSettings)
        .AddAuroreTelemetry(appSettings.AppInformation, appSettings.Telemetry)
        .AddScalar(appSettings)
        .AddAuroreCorsPolicies(appSettings.CorsPolicies)
        ;

    AppDiagnostics.Initialize(appSettings.AppInformation.ActivitySource);

    builder.Services
        .AddMemoryCache()
        .AddHttpClient()
        .AddHttpContextAccessor()
        .AddRequestContext()
        .AddAuroreApiVersioning()
        .AddAuroreResponseCompression()
        .AddAuroreAuthentication(appSettings.IdentityProvider)
        .AddAuroreAuthorization()
        .AddAuroreHealthChecks(appSettings.AppInformation, appSettings.HealthChecks, appSettings.Dependencies)
        // app validators
        // feature deps
        // infra deps
        // request pipeline
        // maintenance mode
        // error handler
        ;

    var app = builder.Build();

    app.UseScalar(appSettings);

    app
        .UseForwardedHeaders()
        // unexpected error
        // security headers
        .UseRequestContext()
        // request enricher
        // maintenance mode
        // request timeouts (UseRequestTimeouts requires AddRequestTimeouts; add both together)
        // body size limit
        .UseHttpsRedirection()
        .UseCors()
        .UseAuthentication()
        .UseAuthorization()
        .UseResponseCompression()
        // endpoint logging
        // rate limiter (UseRateLimiter requires AddRateLimiter; add both when the app needs rate limiting)
        .UseOutputCache();

    app.MapAuroreHealthChecks(appSettings.AppInformation);

    app.Logger.LogInformation("Application is starting up...");

    app.Run();

    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Console.Error.WriteLine($"Application failed with message: {ex}");

    return ApplicationErrors.ConfigurationError;
}
