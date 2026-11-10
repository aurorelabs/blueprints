namespace Aurore.Foundation.Core.Application.Constants;

/// <summary>
/// Process exit codes, following sysexits.h.
/// </summary>
public static class ExitCodes
{
    /// <summary>The process finished normally.</summary>
    public const int Success = 0; // EX_OK
    /// <summary>The application started and then failed.</summary>
    public const int RuntimeError = 70; // EX_SOFTWARE: the app started and then failed
    /// <summary>The application could not start because its configuration is invalid.</summary>
    public const int ConfigurationError = 78; // EX_CONFIG: the app could not start
}
