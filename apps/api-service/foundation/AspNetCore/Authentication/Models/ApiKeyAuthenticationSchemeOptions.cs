using System.Collections.Generic;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.Core.Authentication.Models;
using Microsoft.AspNetCore.Authentication;

namespace Aurore.Foundation.AspNetCore.Authentication.Models;

/// <summary>Options of the API key authentication scheme, filled from the configured API keys.</summary>
public sealed class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    /// <summary>The request header that carries the key.</summary>
    public string HeaderName { get; set; } = ApiKeyDefaults.HeaderName;
    /// <summary>The clients that may call the service, by name.</summary>
    public IReadOnlyDictionary<string, ApiKeyClientOptions> Clients { get; set; } = new Dictionary<string, ApiKeyClientOptions>();
}
