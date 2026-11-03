using System;
using Aurore.Blueprints.ApiService.App.Api.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

try
{
    var builder = WebApplication.CreateBuilder(args);

    var app = builder.Build();

    app.UseHttpsRedirection();

    app.Logger.LogInformation("Application is starting up...");

    app.Run();

    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Console.Error.WriteLine($"Application failed with message: {ex}");

    return ApplicationErrors.ConfigurationError;
}