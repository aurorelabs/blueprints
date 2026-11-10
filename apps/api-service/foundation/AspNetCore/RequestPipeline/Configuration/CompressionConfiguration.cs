using System.IO.Compression;
using System.Linq;
using System.Net.Mime;
using Aurore.Foundation.Core.RequestPipeline.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Configuration;

/// <summary>Registers response compression.</summary>
public static class CompressionConfiguration
{
    internal static IServiceCollection AddAuroreResponseCompression(this IServiceCollection services, ResponseCompressionConfigurationOptions options)
    {
        if (options.Enabled is false)
            return services;

        return services
            .AddResponseCompression(o =>
            {
                o.EnableForHttps = options.EnableForHttps;

                o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([MediaTypeNames.Application.ProblemJson]);

                o.Providers.Add<BrotliCompressionProvider>();
                o.Providers.Add<GzipCompressionProvider>();
            })
            .Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
    }

    /// <summary>
    /// Pairs with AddAuroreResponseCompression: when compression is disabled its services aren't registered,
    /// and the framework's UseResponseCompression() would fail to start.
    /// </summary>
    internal static IApplicationBuilder UseAuroreResponseCompression(this IApplicationBuilder builder, ResponseCompressionConfigurationOptions options)
    {
        return options.Enabled ? builder.UseResponseCompression() : builder;
    }
}
