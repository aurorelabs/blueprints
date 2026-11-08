namespace Aurore.Foundation.Core.Application.Constants;

/// <summary>
/// Process exit codes, following sysexits.h.
/// </summary>
public static class ExitCodes
{
    public const int Success = 0; // EX_OK
    public const int RuntimeError = 70; // EX_SOFTWARE: the app started and then failed
    public const int ConfigurationError = 78; // EX_CONFIG: the app could not start
}
