using System;
using System.Linq;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.Core.Features;

/// <summary>
/// The decorators of one feature instance. They are resolved the first time the feature runs and put in order, so a feature
/// that is never run costs nothing.
/// </summary>
internal sealed class FeaturePipeline(IServiceProvider services, Type[] decoratorTypes)
{
    private IFeatureDecorator[]? _decorators;

    internal IServiceProvider Services => services;

    internal ValueTask<Result> RunAsync(FeatureContext context, FeatureDelegate handler)
    {
        _decorators ??= [.. decoratorTypes.Select(x => (IFeatureDecorator)services.GetRequiredService(x)).OrderBy(x => x.Order)];

        var next = handler;

        for (var index = _decorators.Length - 1; index >= 0; index--)
        {
            var decorator = _decorators[index];
            var inner = next;
            next = () => decorator.InvokeAsync(context, inner);
        }

        return next();
    }
}
