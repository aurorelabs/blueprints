using Microsoft.Extensions.DependencyInjection;
using Tests.Core.Features.Support;

namespace Tests.Core.Features.Configuration;

[Trait("Feature", "Feature registration")]
public sealed class FeatureConfigurationTests
{
    [Fact(DisplayName = "A feature built by hand runs its logic alone, with no pipeline")]
    public async Task FeatureBuiltByHandRunsAlone()
    {
        // Arrange
        var trace = new Trace();
        var feature = new PlainFeature(trace);

        // Act
        var result = await feature.HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal("hello ana", result.Value);
        Assert.Equal(["feature"], trace.Steps);
    }

    [Fact(DisplayName = "Nothing is enabled unless asked for, so a plain feature runs alone")]
    public async Task NothingIsEnabledByDefault()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider();
        var feature = provider.GetRequiredService<PlainFeature>();

        // Act
        await feature.HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["feature"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A decorator enabled for all runs around every feature")]
    public async Task DecoratorEnabledForAllRunsAroundEveryFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features.With<FirstDecorator>());

        // Act
        await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["first:before", "feature", "first:after"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A feature can enable a decorator for itself with the With attribute")]
    public async Task WithAttributeEnablesADecoratorForOneFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider();

        // Act
        await provider.GetRequiredService<FirstFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["first:before", "feature", "first:after"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A feature can switch off a decorator enabled for all with the Without attribute")]
    public async Task WithoutAttributeSwitchesADecoratorOff()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features.With<FirstDecorator>());

        // Act
        await provider.GetRequiredService<QuietFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["feature"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "When a feature both enables and disables a decorator, disabling wins")]
    public async Task WithoutWinsOverWith()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider();

        // Act
        await provider.GetRequiredService<ContradictoryFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(["feature"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "Decorators run by their order, not by the order they were enabled in, and a custom one defaults to last")]
    public async Task DecoratorsRunByTheirOrder()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features
            .With<DefaultOrderDecorator>()
            .With<SecondDecorator>()
            .With<FirstDecorator>());

        // Act
        await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Equal(
            ["first:before", "second:before", "default:before", "feature", "second:after", "first:after"],
            provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A decorator that refuses stops the feature and its failure comes back as the feature's own result type")]
    public async Task RefusingDecoratorStopsTheFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features.With<RefusingDecorator>());

        // Act
        var withValue = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);
        var command = await provider.GetRequiredService<CommandFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.Same(RefusingDecorator.Refused, withValue.Error);
        Assert.Same(RefusingDecorator.Refused, command.Error);
        Assert.Empty(provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "Features are registered as themselves, once per scope, and classes that are not features are left out")]
    public void FeaturesAreRegisteredAsThemselvesPerScope()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        // Act
        var a = first.ServiceProvider.GetRequiredService<PlainFeature>();
        var b = first.ServiceProvider.GetRequiredService<PlainFeature>();
        var c = second.ServiceProvider.GetRequiredService<PlainFeature>();

        // Assert
        Assert.Same(a, b);
        Assert.NotSame(a, c);
        Assert.Null(provider.GetService<AbstractFeature>());
        Assert.Null(provider.GetService<NotAFeature>());
    }
}
