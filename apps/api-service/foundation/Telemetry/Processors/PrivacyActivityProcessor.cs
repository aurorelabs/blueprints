using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Aurore.Foundation.Core.Privacy;
using OpenTelemetry;

namespace Aurore.Foundation.Telemetry.Processors;

/// <summary>
/// Encrypts protected tags (client.address, user_agent.original, ...) when a span ends, before any exporter sees it.
/// The ASP.NET Core instrumentation adds those tags with real values on its own, so they have to be rewritten here.
/// </summary>
public sealed class PrivacyActivityProcessor(IPrivacyProtector privacy) : BaseProcessor<Activity>
{
    /// <summary>Encrypts the protected tags of the span that just ended.</summary>
    public override void OnEnd(Activity data)
    {
        List<KeyValuePair<string, object?>> protectedTags = [.. data.TagObjects.Where(x => x.Value is string && privacy.IsProtected(x.Key))];

        foreach (var (key, value) in protectedTags)
        {
            data.SetTag(key, privacy.Protect(key, (string)value!));
        }
    }
}
