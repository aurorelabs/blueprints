namespace Aurore.Foundation.Core.Constants;

public static class TimestampFormats
{
    public const string TimeOnly = "HH:mm:ss";

    /// <summary>
    /// Same as <see cref="TimeOnly"/> with a trailing space. The simple console formatter writes the timestamp
    /// directly in front of the log level without a separator, so the space keeps them from running together.
    /// </summary>
    public const string TimeOnlyWithTrailingSpace = $"{TimeOnly} ";

    public static class Iso8601
    {
        public const string DateOnly = "yyyy-MM-dd";

        /// <summary>Date and time without a UTC marker or offset, so the value is ambiguous across time zones.</summary>
        public const string DateTimeWithoutOffset = "yyyy-MM-ddTHH:mm:ss";

        /// <summary>UTC date and time with millisecond precision, e.g. 2026-11-07T14:03:25.412Z.</summary>
        public const string UtcWithMilliseconds = "yyyy-MM-ddTHH:mm:ss.fffZ";
    }
}
