using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Extensions;

namespace Aurore.Foundation.Core.Configuration;

/// <summary>
/// The JSON conventions shared by every part of the foundation: camelCase, enums as strings, cycles ignored, compact
/// output.
/// </summary>
public static class JsonSerializerConfiguration
{
    /// <summary>Returns a new options object with the shared conventions applied.</summary>
    public static JsonSerializerOptions Default()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        Configure(options);

        return options;
    }

    /// <summary>
    /// Applies the shared conventions to an existing options object, for example the one ASP.NET Core uses.
    /// </summary>
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

    /// <summary>
    /// Serializes the object with the shared conventions; the optional action adjusts them for this call.
    /// </summary>
    public static string Serialize(this object source, Action<JsonSerializerOptions>? optionsAction = null)
    {
        return JsonSerializer.Serialize(source, BuildOptions(optionsAction));
    }

    /// <summary>Writes the object to a file as indented JSON unless the options action says otherwise.</summary>
    public static void SerializeToFile(this object source, string filePath, Action<JsonSerializerOptions>? optionsAction = null)
    {
        filePath = Path.NormalizeRuntimePath(filePath);

        File.WriteAllText(filePath, source.Serialize(optionsAction is null ? x => x.WriteIndented = true : optionsAction));
    }

    /// <summary>Writes the value to the stream with the shared conventions.</summary>
    public static async Task SerializeAsync<T>(this Stream source, T target, Action<JsonSerializerOptions>? optionsAction = null, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(source, target, BuildOptions(optionsAction), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads a value from JSON text with the shared conventions.</summary>
    public static T Deserialize<T>(this string source, Action<JsonSerializerOptions>? optionsAction = null)
    {
        return JsonSerializer.Deserialize<T>(source, BuildOptions(optionsAction))!;
    }

    /// <summary>Reads a value from a stream with the shared conventions.</summary>
    public static async ValueTask<T> DeserializeAsync<T>(this Stream source, Action<JsonSerializerOptions>? optionsAction = null, CancellationToken cancellationToken = default)
    {
        return (await JsonSerializer.DeserializeAsync<T>(source, BuildOptions(optionsAction), cancellationToken).ConfigureAwait(false))!;
    }

    /// <summary>
    /// Reads a value from a JSON file. A missing file throws, or returns the default value when throwOnError is false.
    /// </summary>
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
