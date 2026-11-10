namespace Aurore.Foundation.Core.Privacy;

/// <summary>
/// Encrypts personal data on its way to logs and traces, and decrypts it again for the people allowed to read it.
/// </summary>
public interface IPrivacyProtector
{
    /// <summary>
    /// True when values of this field are encrypted before leaving the process (logs, trace tags).
    /// </summary>
    public bool IsProtected(string field);

    /// <summary>
    /// Returns the value encrypted when protection is on and the field is protected; otherwise the value unchanged.
    /// </summary>
    public string? Protect(string field, string? value);

    /// <summary>
    /// Decrypts a token produced by Protect with the current key or one of the previous keys. Returns null when the
    /// token is malformed, was made with an unknown key, or was tampered with.
    /// </summary>
    public string? Unprotect(string token);
}
