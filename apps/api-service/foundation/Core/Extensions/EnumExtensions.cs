using System;

namespace Aurore.Foundation.Core.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Case-insensitive. Throws on unknown values (including numbers that map to no member), so a configuration
    /// typo fails at startup instead of silently becoming the enum's first member.
    /// </summary>
    public static TEnum ParseEnum<TEnum>(this string? value)
        where TEnum : struct, Enum
    {
        if (TryParseDefined<TEnum>(value, out var result))
            return result;

        throw new InvalidOperationException(
            $"'{value}' is not a valid {typeof(TEnum).Name} value. Expected one of: {string.Join(", ", Enum.GetNames<TEnum>())}.");
    }

    public static TEnum ParseEnumOrDefault<TEnum>(this string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        return TryParseDefined<TEnum>(value, out var result) ? result : fallback;
    }

    private static bool TryParseDefined<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result);
    }
}
