using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.Extensions.Hosting;

namespace Aurore.Blueprints.ApiService.App.Api.Resources.Tests;

internal sealed class TestsResource : ResourceGroup
{
    public override bool RequireAuthorization => false;

    public override bool IsAvailable(IHostEnvironment environment)
    {
        return environment.IsProduction() is false;
    }
}
