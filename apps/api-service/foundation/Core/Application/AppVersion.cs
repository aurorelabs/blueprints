using System.Reflection;
using Aurore.Foundation.Core.Extensions;

namespace Aurore.Foundation.Core.Application;

/// <summary>The running application's version.</summary>
public static class AppVersion
{
    /// <summary>
    /// The entry assembly's informational version: &lt;Version&gt; in the app's csproj, or `dotnet publish -p:Version=1.4.2`
    /// in CI. The SDK appends "+&lt;commit sha&gt;" to it; that suffix is dropped.
    /// </summary>
    public static string Current { get; } = Read();

    private static string Read()
    {
        var version = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return version.HasValue() ? version.Split('+')[0] : "0.0.0";
    }
}
