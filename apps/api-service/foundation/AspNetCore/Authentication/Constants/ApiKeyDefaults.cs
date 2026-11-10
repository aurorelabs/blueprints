namespace Aurore.Foundation.AspNetCore.Authentication.Constants;

/// <summary>Defaults of the API key authentication scheme.</summary>
public static class ApiKeyDefaults
{
    /// <summary>The name of the API key authentication scheme.</summary>
    public const string AuthenticationScheme = "ApiKey";
    /// <summary>The request header the key is read from unless configured otherwise.</summary>
    public const string HeaderName = "X-Api-Key";
}
