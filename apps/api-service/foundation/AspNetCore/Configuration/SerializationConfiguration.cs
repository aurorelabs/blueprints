using Aurore.Foundation.Core.Configuration;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aurore.Foundation.AspNetCore.Configuration;

public static class SerializationConfiguration
{
    public static IHostApplicationBuilder ConfigureAuroreSerializers(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<JsonOptions>(o =>
            JsonSerializerConfiguration.Configure(o.SerializerOptions));

        return builder;
    }
}
