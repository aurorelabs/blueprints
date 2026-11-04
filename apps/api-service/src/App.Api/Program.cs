using System;
using Aurore.Blueprints.ApiService.App.Api.Configuration;
using Aurore.Blueprints.ApiService.Features.Configuration;
using Aurore.Blueprints.ApiService.Infra.Configuration;
using Aurore.Foundation.AspNetCore.Authentication.Configuration;
using Aurore.Foundation.AspNetCore.Configuration;
using Aurore.Foundation.AspNetCore.HealthChecks.Configuration;
using Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;
using Aurore.Foundation.Core.Application;
using Aurore.Foundation.Core.Application.Constants;
using Aurore.Foundation.Telemetry.Configuration;
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
        .AddAppScalar(appSettings)
        .AddAuroreCorsPolicies(appSettings.CorsPolicies);

    AppDiagnostics.Initialize(appSettings.AppInformation.ActivitySource);

    var pipeline = appSettings.RequestPipeline;

    builder.Services
        .ConfigureAuroreKestrelLimits(pipeline.KestrelLimits)
        .ConfigureAuroreForwardedHeaders(pipeline.ForwardedHeaders)
        .AddAuroreRequestContext()
        .AddAuroreApiVersioning()
        .AddAuroreResponseCompression(pipeline.ResponseCompression)
        .AddAuroreAuthentication(appSettings.IdentityProvider, appSettings.ApiKeys)
        .AddAuthorization()
        .AddAuroreHealthChecks(appSettings.AppInformation, appSettings.HealthChecks, appSettings.Dependencies)
        .AddAuroreRequestTimeouts(pipeline.Timeouts)
        .AddAuroreRateLimiting(pipeline.RateLimiting)
        .AddAuroreIdempotency(pipeline.Idempotency)
        .AddAuroreExceptionHandler()
        .AddAppValidators(appSettings)
        .AddAppFeatures(appSettings)
        .AddAppInfra(appSettings);

    var app = builder.Build();

    app.UseAppScalar(appSettings);

    app
        .UseForwardedHeaders()
        .UseExceptionHandler()
        .UseAuroreSecurityHeaders(pipeline.SecurityHeaders)
        .UseAuroreRequestContext()
        .UseHttpsRedirection()
        .UseAuroreResponseCompression(pipeline.ResponseCompression)
        .UseCors()
        .UseAuroreMaintenanceMode(appSettings.MaintenanceMode)
        .UseAuthentication()
        .UseAuthorization()
        .UseRateLimiter()
        .UseRequestTimeouts()
        .UseOutputCache();

    app.MapAuroreHealthChecks(appSettings.AppInformation);

    app.MapAppResources();

    app.Logger.LogInformation("Application is starting up...");

    app.Run();

    return ExitCodes.Success;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Console.Error.WriteLine($"Application failed with message: {ex}");

    return ExitCodes.ConfigurationError;
}
