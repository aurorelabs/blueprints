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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

WebApplication app;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder
        .ConfigureAuroreSerializers()
        .LoadAppSettings(out var appSettings)
        .ConfigureAuroreHosting(appSettings.Hosting)
        .AddAuroreTelemetry(appSettings.AppInformation, appSettings.Telemetry)
        .AddAuroreMinimalEndpoints<Program>(out var endpoints)
        .AddAppScalar(appSettings, endpoints)
        .AddAuroreRequestPipeline(appSettings.RequestPipeline);

    AppDiagnostics.Initialize(appSettings.AppInformation.ActivitySource);

    builder.Services
        .AddSingleton(TimeProvider.System)
        .AddSingleton(appSettings.Pagination)
        .AddAuroreApiVersioning(endpoints)
        .AddAuroreAuthConfiguration(appSettings.IdentityProvider, appSettings.ApiKeys)
        .AddAuroreHealthChecks(appSettings.AppInformation, appSettings.HealthChecks, appSettings.HttpDependencies)
        .AddAppValidators(appSettings)
        .AddAppFeatures()
        .AddAppInfra(appSettings);

    app = builder.Build();

    app
        .UseAuroreRequestPipeline(appSettings.RequestPipeline)
        .UseAuthentication()
        .UseAuthorization()
        .UseAuroreEndpointPolicies();

    app.MapAppScalar(appSettings);

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
