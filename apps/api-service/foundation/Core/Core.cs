using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Aurore.Foundation.Core;

public static class ApplicationErrors
{
    public const int ConfigurationError = 78; // CONFIG (sysexits.h)
}

public static class AppDiagnostics
{
    private static readonly Lock _lock = new();
    private static ActivitySource? _source;

    public static ActivitySource Source
    {
        get
        {
            return _source ?? throw new InvalidOperationException(
                $"{nameof(AppDiagnostics)} is not initialized. Call {nameof(Initialize)} at startup.");
        }
    }

    public static void Initialize(string sourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        lock (_lock)
        {
            if (_source is null)
            {
                _source = new ActivitySource(sourceName);
                return;
            }

            if (_source.Name != sourceName)
                throw new InvalidOperationException($"{nameof(AppDiagnostics)} is already initialized with source '{_source.Name}'.");
        }
    }

    public static Activity? StartActivity(this string name, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        return _source?.StartActivity(name, ActivityKind.Internal, parentContext: default, tags);
    }
}

public record AppInformationOptions
{
    public required string Version { get; init; }
    public required string Domain { get; init; }
    public required string Boundary { get; init; }
    public required string Component { get; init; }
    public required string Description { get; init; }
    public required AppMaintainerOptions Maintainer { get; init; }
    public string Locale { get; init; } = "en-US";
    public string Namespace => $"{Domain}.{Boundary}";
    public string ActivitySource => $"{Namespace}.{Component}";
}

//public record EnvironmentOptions(string Name, bool IsProduction);

public record AppMaintainerOptions(string Name, string Email);

public sealed record IdentityProviderOptions
{
    public required string AuthorityUrl { get; init; }
    public required string Audience { get; init; }
    public Dictionary<string, string> Scopes { get; init; } = [];
    public bool RequireHttpsMetadata { get; init; } = true;
}

public static class StringExtensions
{
    public static bool HasValue([NotNullWhen(true)] this string? value)
    {
        return string.IsNullOrWhiteSpace(value) is false;
    }

    public static string RemoveDiacritics(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool IsEqualTo([NotNullWhen(false)] this string value, string target)
    {
        return string.Equals(value, target, StringComparison.InvariantCultureIgnoreCase);
    }

    public static string[] SplitWords(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var words = new List<string>();
        var start = -1;

        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsLetterOrDigit(value[i]))
            {
                if (start < 0)
                    start = i;

                continue;
            }

            if (start >= 0)
            {
                words.Add(value[start..i]);
                start = -1;
            }
        }

        if (start >= 0)
            words.Add(value[start..]);

        return [.. words];
    }

    public static string ToSentenceCase(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }

    public static string ToTitleCase(this string? value)
    {
        return string.Join(' ', value.SplitWords().Select(ToSentenceCase));
    }

    public static string ToSnakeCase(this string? value, bool lowercase = true)
    {
        return JoinWords(value.RemoveDiacritics(), '_', lowercase);
    }

    public static string ToSlugCase(this string? value, bool lowercase = true)
    {
        return JoinWords(value.RemoveDiacritics(), '-', lowercase);
    }

    private static string JoinWords(string value, char separator, bool lowercase)
    {
        var words = value.SplitWords();

        return lowercase
            ? string.Join(separator, words).ToLowerInvariant()
            : string.Join(separator, words.Select(ToSentenceCase));
    }
}

public static class EnumExtensions
{
    public static TEnum ParseToEnum<TEnum>(this string value, TEnum? defaultValue = null)
        where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(value, out var result))
            return result;

        return defaultValue ?? default;
    }

    public static T TryParseEnum<T>([NotNullWhen(false)] this string? value, T? fallbackValue = null) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var result))
            return result;

        return fallbackValue ?? throw new InvalidCastException($"'{value}' is not a valid '{typeof(T).Name}' enum value.");
    }
}

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

    public static T DeserializeFromFile<T>(this string filename, bool throwOnError = true)
    {
        if (File.Exists(filename) is false && throwOnError)
        {
            throw new FileNotFoundException($"The file '{filename}' was not found.");
        }

        return File.ReadAllText(filename).Deserialize<T>();
    }

    private static JsonSerializerOptions BuildOptions(Action<JsonSerializerOptions>? optionsAction)
    {
        var options = Default();

        optionsAction?.Invoke(options);

        return options;
    }
}

public static class OperatingSystemExtensions
{
    extension(OperatingSystem)
    {
        public static bool IsWsl()
        {
            return
                OperatingSystem.IsLinux() &&
                Environment.GetEnvironmentVariable("WSL_DISTRO_NAME") is not null;
        }

        public static bool IsNativeLinux()
        {
            return OperatingSystem.IsLinux() && IsWsl() is false;
        }
    }
}

public static class PathExtensions
{
    private const string WslPrefix = "/mnt/";

    extension(Path)
    {
        public static string NormalizeRuntimePath(params string[] paths)
        {
            var normalized = paths
                .Select(NormalizeSegment)
                .ToArray();

            return Path.GetFullPath(Path.Combine(normalized));
        }

        [SuppressMessage("Style", "IDE0051", Justification = "Used above via a method-group conversion (.Select(NormalizeSegment)) — IDE0051 doesn't trace usages of a private member declared inside an extension(...) block through a method group, only through explicit invocation syntax, so it's a false positive.")]
        private static string NormalizeSegment(string path)
        {
            if (OperatingSystem.IsWindows())
                return FromWslPath(path);

            return OperatingSystem.IsWsl()
                ? ToWslPath(path)
                : path;
        }

        private static string FromWslPath(string path)
        {
            if (!path.StartsWith(WslPrefix, StringComparison.OrdinalIgnoreCase))
                return path;

            // /mnt/d/git/repo -> D:\git\repo
            var drive = char.ToUpperInvariant(path[5]);

            var remainder = path[6..]
                .Replace('/', '\\');

            return $"{drive}:\\{remainder}";
        }

        private static string ToWslPath(string path)
        {
            if (path.Length < 2 || path[1] != ':' || !char.IsAsciiLetter(path[0]))
                return path;

            // D:\git\repo -> /mnt/d/git/repo
            var drive = char.ToLowerInvariant(path[0]);

            var remainder = path[2..]
                .Replace('\\', '/')
                .TrimStart('/');

            return $"/mnt/{drive}/{remainder}";
        }
    }
}

public sealed class RequestContext
{
    public string CorrelationId { get; private set; } = string.Empty;

    public string TraceId { get; private set; } = string.Empty;

    public string? SpanId { get; private set; }

    public string? OriginalSpanId { get; private set; }

    public string? IdempotencyKey { get; private set; }

    public void Update(string correlationId, string traceId, string? spanId)
    {
        CorrelationId = correlationId;
        TraceId = traceId;
        SpanId = spanId;
        OriginalSpanId ??= spanId;
    }

    public void UpdateSpan(string? spanId)
    {
        SpanId = spanId;
    }

    public void UpdateIdempotencyKey(string? idempotencyKey)
    {
        IdempotencyKey = idempotencyKey;
    }
}

public sealed record CorsPolicyOptions
{
    public string[] AllowedHeaders { get; init; } = [];
    public string[] AllowedMethods { get; init; } = [];
    public string[] AllowedOrigins { get; init; } = [];
    public string[] ExposedHeaders { get; init; } = [];
    public bool AllowCredentials { get; init; }
    public int PreflightMaxAgeSeconds { get; init; }
}

public static class HealthCheckProperties
{
    public static class Headers
    {
        public const string Client = "AL-HealthCheck-Client";
        public const string AuthorizationKey = "AL-HealthCheck-Authorization-Key";
    }

    public static class Endpoints
    {
        public const string Liveness = "/health/live";
        public const string Readiness = "/health/ready";
        public const string Status = "/health/status";
    }

    public static class Configuration
    {
        public const int TimeoutInSeconds = 10;
        public const string OutputCacheKey = "health-checks-cache";
        public const string RateLimitingKey = "health-checks-rate-limiting-checks";
    }

    public static class Tags
    {
        public const string Liveness = "liveness";
        public const string Readiness = "readiness";
        public const string Critical = "critical";
        public const string NonCritical = "non-critical";
        public const string TypeDatabase = "type-database";
        public const string TypeHttpService = "type-httpservice";
    }
}

public record HealthCheckConfigurationOptions
{
    public string Token { get; set; } = string.Empty; // AI: Not sure about this thing, easier to limit endpoints with cors, no? or other way, I don't know. Seems weird. Maybe use headers instead? Look for something in the header? I don't know
    public int RateLimitPermitCount { get; init; } = 5;
    public int RateLimitWindowSeconds { get; init; } = 10;
    public int ReadinessCacheSeconds { get; init; } = 5;
}

public sealed record HealthCheckEndpointOptions
{
    public required string ServiceName { get; init; }
    public required string Version { get; init; }
    public required string Token { get; init; }
}

public sealed record HttpServiceDependencyOptions
{
    public required string BaseUrl { get; init; }
    public Dictionary<string, object> RequestHeaders { get; init; } = [];
    public HttpServiceHealthCheckOptions? HealthCheck { get; init; }
}

public sealed record HttpServiceHealthCheckOptions
{
    public string? ReadinessEndpoint { get; init; }
    public int TimeoutSeconds { get; init; }
    public bool IsCritical { get; init; }
    public List<string> Tags { get; init; } = [];
}
