using System.Collections.Generic;
using Aurore.Foundation.AspNetCore.Authentication.Constants;
using Aurore.Foundation.Core.Authentication.Models;
using Microsoft.AspNetCore.Authentication;

namespace Aurore.Foundation.AspNetCore.Authentication.Models;

public sealed class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = ApiKeyDefaults.HeaderName;
    public IReadOnlyDictionary<string, ApiKeyClientOptions> Clients { get; set; } = new Dictionary<string, ApiKeyClientOptions>();
}
