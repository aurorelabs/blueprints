using System;

namespace Aurore.Foundation.AspNetCore.MinimalApis;

/// <summary>
/// An API version a resource group exposes. A plain int converts implicitly, so <c>[1, 2]</c> works, and
/// <c>[1, new(2, IsDeprecated: true, Sunset: new DateOnly(2027, 1, 1), Description: "...")]</c> adds the details.
/// </summary>
/// <param name="Number">The version number (1 is v1).</param>
/// <param name="IsDeprecated">Advertised as deprecated in the api-deprecated-versions header and in the API document.</param>
/// <param name="Sunset">Date the version stops being served; sent in the Sunset header. Requires <paramref name="IsDeprecated"/>.</param>
/// <param name="Description">What this version is about; shown in the API document. Shared by every group declaring the version.</param>
public readonly record struct MinimalEndpointVersion(int Number, bool IsDeprecated = false, DateOnly? Sunset = null, string? Description = null)
{
    public static implicit operator MinimalEndpointVersion(int number)
    {
        return new MinimalEndpointVersion(number);
    }

    public override string ToString()
    {
        return IsDeprecated ? $"{Number} (deprecated)" : $"{Number}";
    }
}
