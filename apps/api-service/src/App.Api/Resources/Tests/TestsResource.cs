using Aurore.Foundation.AspNetCore.MinimalApis;

namespace Aurore.Blueprints.ApiService.App.Api.Resources.Tests;

internal sealed class TestsResource : ResourceGroup
{
    public override bool RequireAuthorization => false;
}
