using System.Diagnostics.CodeAnalysis;

namespace Aurore.Foundation.Core.Application.Models;

/// <summary>
/// The team that owns the service. <paramref name="Url"/> is the optional contact page shown in the API documents.
/// </summary>
[ExcludeFromCodeCoverage]
public record AppMaintainerOptions(string Name, string Email, string? Url = null);
