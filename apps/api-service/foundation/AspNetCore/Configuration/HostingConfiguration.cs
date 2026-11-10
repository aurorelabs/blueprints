using System;
using Aurore.Foundation.Core.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aurore.Foundation.AspNetCore.Configuration;

/// <summary>Configures the host process.</summary>
public static class HostingConfiguration
{
    /// <summary>
    /// Applies the hosting settings, such as how long the host waits for in-flight requests when it stops.
    /// </summary>
    public static IHostApplicationBuilder ConfigureAuroreHosting(this IHostApplicationBuilder builder, HostingOptions options)
    {
        builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(options.ShutdownTimeoutSeconds));

        return builder;
    }
}
