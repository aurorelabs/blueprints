using Aurore.Foundation.Core.Configuration;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aurore.Foundation.AspNetCore.Configuration;

/// <summary>Configures how the API reads and writes JSON.</summary>
public static class SerializationConfiguration
{
    /// <summary>Makes the minimal API JSON options follow the shared conventions (camelCase, enums as strings).</summary>
    public static IHostApplicationBuilder ConfigureAuroreSerializers(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<JsonOptions>(o =>
            JsonSerializerConfiguration.Configure(o.SerializerOptions));

        return builder;
    }
}
