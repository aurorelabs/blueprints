using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Extensions;

namespace Aurore.Foundation.Core.Configuration;

public static class JsonSerializerConfiguration
{
    public static JsonSerializerOptions Default()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        Configure(options);

        return options;
    }

    public static void Configure(JsonSerializerOptions options)
    {
        options.AllowTrailingCommas = true;
        options.PropertyNameCaseInsensitive = true;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.WriteIndented = false;
        options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        options.Converters.Clear();
        options.Converters.Add(new JsonStringEnumConverter());
    }

    public static string Serialize(this object source, Action<JsonSerializerOptions>? optionsAction = null)
    {
        return JsonSerializer.Serialize(source, BuildOptions(optionsAction));
    }

    public static void SerializeToFile(this object source, string filePath, Action<JsonSerializerOptions>? optionsAction = null)
    {
        filePath = Path.NormalizeRuntimePath(filePath);

        File.WriteAllText(filePath, source.Serialize(optionsAction is null ? x => x.WriteIndented = true : optionsAction));
    }

    public static async Task SerializeAsync<T>(this Stream source, T target, Action<JsonSerializerOptions>? optionsAction = null, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(source, target, BuildOptions(optionsAction), cancellationToken).ConfigureAwait(false);
    }

    public static T Deserialize<T>(this string source, Action<JsonSerializerOptions>? optionsAction = null)
    {
        return JsonSerializer.Deserialize<T>(source, BuildOptions(optionsAction))!;
    }

    public static async ValueTask<T> DeserializeAsync<T>(this Stream source, Action<JsonSerializerOptions>? optionsAction = null, CancellationToken cancellationToken = default)
    {
        return (await JsonSerializer.DeserializeAsync<T>(source, BuildOptions(optionsAction), cancellationToken).ConfigureAwait(false))!;
    }

    public static T? DeserializeFromFile<T>(this string filename, bool throwOnError = true)
    {
        if (File.Exists(filename) is false)
            return throwOnError ? throw new FileNotFoundException($"The file '{filename}' was not found.") : default;

        return File.ReadAllText(filename).Deserialize<T>();
    }

    private static JsonSerializerOptions BuildOptions(Action<JsonSerializerOptions>? optionsAction)
    {
        var options = Default();

        optionsAction?.Invoke(options);

        return options;
    }
}
