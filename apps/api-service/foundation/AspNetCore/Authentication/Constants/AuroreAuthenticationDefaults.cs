namespace Aurore.Foundation.AspNetCore.Authentication.Constants;

/// <summary>Names of the authentication schemes the foundation registers.</summary>
public static class AuroreAuthenticationDefaults
{
    /// <summary>
    /// Default scheme when API keys are enabled: forwards to ApiKey when the key header is present, otherwise to Bearer.
    /// </summary>
    public const string BearerOrApiKeyScheme = "BearerOrApiKey";
}
