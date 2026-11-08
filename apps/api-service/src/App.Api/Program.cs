using System;
using Aurore.Blueprints.ApiService.App.Api.Configuration;
using Aurore.Blueprints.ApiService.Features.Configuration;
using Aurore.Blueprints.ApiService.Infra.Configuration;
using Aurore.Foundation.AspNetCore.Authentication.Configuration;
using Aurore.Foundation.AspNetCore.Configuration;
using Aurore.Foundation.AspNetCore.HealthChecks.Configuration;
using Aurore.Foundation.AspNetCore.MinimalApis.Configuration;
using Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;
using Aurore.Foundation.Core.Application;
using Aurore.Foundation.Core.Application.Constants;
using Aurore.Foundation.Telemetry.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

WebApplication app;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder
        .ConfigureAuroreSerializers()
        .LoadAppSettings(out var appSettings)
        .AddAuroreTelemetry(appSettings.AppInformation, appSettings.Telemetry)
        .AddAuroreMinimalEndpoints<Program>(out var endpoints)
        .AddAppScalar(appSettings, endpoints)
        .AddAuroreCorsPolicies(appSettings.RequestPipeline.CorsPolicies);

    AppDiagnostics.Initialize(appSettings.AppInformation.ActivitySource);

    var pipeline = appSettings.RequestPipeline;

    builder.Services
        .ConfigureAuroreKestrelLimits(pipeline.KestrelLimits)
        .ConfigureAuroreForwardedHeaders(pipeline.ForwardedHeaders)
        .AddAuroreRequestContext()
        .AddAuroreApiVersioning(endpoints)
        .AddAuroreResponseCompression(pipeline.ResponseCompression)
        .AddAuroreAuthConfiguration(appSettings.IdentityProvider, appSettings.ApiKeys)
        .AddAuroreHealthChecks(appSettings.AppInformation, appSettings.HealthChecks, appSettings.HttpDependencies)
        .AddAuroreRequestTimeouts(pipeline.Timeouts)
        .AddAuroreRateLimiting(pipeline.RateLimiting)
        .AddAuroreIdempotency(pipeline.Idempotency)
        .AddAuroreExceptionHandler()
        .AddAppValidators(appSettings)
        .AddAppFeatures(appSettings)
        .AddAppInfra(appSettings);

    app = builder.Build();

    app.UseAppScalar(appSettings);

    app
        .UseForwardedHeaders()
        .UseExceptionHandler()
        .UseAuroreSecurityHeaders(pipeline.SecurityHeaders)
        .UseAuroreRequestContext()
        .UseHttpsRedirection()
        .UseAuroreResponseCompression(pipeline.ResponseCompression)
        .UseCors()
        .UseAuroreMaintenanceMode(pipeline.MaintenanceMode)
        .UseAuthentication()
        .UseAuthorization()
        .UseRateLimiter()
        .UseRequestTimeouts()
        .UseOutputCache();

    app.MapAuroreHealthChecks(appSettings.AppInformation);

    app.MapAppResources(endpoints);
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Console.Error.WriteLine($"Application failed to start: {ex}");

    return ExitCodes.ConfigurationError;
}

try
{
    app.Logger.LogInformation("Application is starting up...");

    app.Run();

    return ExitCodes.Success;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Console.Error.WriteLine($"Application stopped unexpectedly: {ex}");

    return ExitCodes.RuntimeError;
}
