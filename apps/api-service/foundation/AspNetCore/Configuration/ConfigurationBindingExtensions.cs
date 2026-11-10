using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Aurore.Foundation.AspNetCore.Configuration;

/// <summary>Binding of configuration sections to settings classes.</summary>
public static class ConfigurationBindingExtensions
{
    /// <summary>
    /// Binds <typeparamref name="T"/> like <c>Get&lt;T&gt;()</c>, but fails on keys it does not know, so a typo in
    /// appsettings is an error instead of a silently ignored value. Only the top-level sections <typeparamref name="T"/>
    /// declares are considered; the rest (Logging, AllowedHosts, environment variables...) belongs to the framework.
    /// </summary>
    public static T GetStrict<T>(this IConfiguration configuration)
        where T : class
    {
        var sections = typeof(T).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var scoped = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration.AsEnumerable().Where(x => sections.Contains(x.Key.Split(':')[0])))
            .Build();

        return scoped.Get<T>(options => options.ErrorOnUnknownConfiguration = true)
            ?? throw new InvalidOperationException($"No configuration found for {typeof(T).Name}.");
    }
}
