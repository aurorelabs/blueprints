using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Aurore.Foundation.Core.Extensions;

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
