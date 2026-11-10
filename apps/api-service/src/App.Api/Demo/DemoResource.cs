using Aurore.Foundation.AspNetCore.MinimalApis;

namespace Aurore.Blueprints.ApiService.App.Api.Demo;

/// <summary>
/// Base for the demo resources: each one showcases a single platform feature under /v1/demo/{topic}.
/// </summary>
internal abstract class DemoResource(string topic, string title) : ResourceGroup
{
    public override string Name => $"demo/{topic}";

    public override string[] Tags => [$"Demo: {title}"];
}
