using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Aurore.Foundation.Core.Extensions;

/// <summary>Text helpers: checks, comparison and case conversion.</summary>
public static class StringExtensions
{
    /// <summary>True when the text is not null, empty or whitespace.</summary>
    public static bool HasValue([NotNullWhen(true)] this string? value)
    {
        return string.IsNullOrWhiteSpace(value) is false;
    }

    /// <summary>Removes accents, so "Ação" becomes "Acao". Null becomes an empty string.</summary>
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

    /// <summary>Case-insensitive, culture-independent comparison.</summary>
    public static bool IsEqualTo(this string? value, string? target)
    {
        return string.Equals(value, target, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Words are runs of letters and digits; case changes also split: "ApiService" -&gt; ["Api", "Service"],
    /// "HTTPServer" -&gt; ["HTTP", "Server"], "Version2Api" -&gt; ["Version2", "Api"]. Brand names split too ("GitHub" -&gt; "Git", "Hub").
    /// </summary>
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
                {
                    start = i;
                }
                else if (IsCaseBoundary(value, i))
                {
                    words.Add(value[start..i]);
                    start = i;
                }

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

    private static bool IsCaseBoundary(string value, int index)
    {
        if (char.IsUpper(value[index]) is false)
            return false;

        var previous = value[index - 1];

        if (char.IsLower(previous) || char.IsDigit(previous))
            return true;

        return char.IsUpper(previous) && index + 1 < value.Length && char.IsLower(value[index + 1]);
    }

    /// <summary>First letter upper case, the rest lower case.</summary>
    public static string ToSentenceCase(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }

    /// <summary>Splits the text into words and capitalizes each: "apiService" becomes "Api Service".</summary>
    public static string ToTitleCase(this string? value)
    {
        return string.Join(' ', value.SplitWords().Select(ToSentenceCase));
    }

    /// <summary>
    /// First word lower case, the others capitalized, the way System.Text.Json writes camel case: "PageSize" becomes
    /// "pageSize". Null becomes an empty string.
    /// </summary>
    public static string ToCamelCase(this string? value)
    {
        return value is null ? string.Empty : JsonNamingPolicy.CamelCase.ConvertName(value);
    }

    /// <summary>
    /// Words joined with underscores, without accents, lower case ("ApiService" becomes "api_service") or upper case
    /// ("API_SERVICE"). Uses the System.Text.Json snake case policy. Null becomes an empty string.
    /// </summary>
    public static string ToSnakeCase(this string? value, bool lowercase = true)
    {
        var policy = lowercase ? JsonNamingPolicy.SnakeCaseLower : JsonNamingPolicy.SnakeCaseUpper;

        return policy.ConvertName(value.RemoveDiacritics());
    }

    /// <summary>
    /// Words joined with hyphens, without accents, lower case ("ApiService" becomes "api-service") or upper case
    /// ("API-SERVICE"). Uses the System.Text.Json kebab case policy. Null becomes an empty string.
    /// </summary>
    public static string ToSlugCase(this string? value, bool lowercase = true)
    {
        var policy = lowercase ? JsonNamingPolicy.KebabCaseLower : JsonNamingPolicy.KebabCaseUpper;

        return policy.ConvertName(value.RemoveDiacritics());
    }
}
