using Aurore.Foundation.AspNetCore.Configuration;
using Aurore.Foundation.Core.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.AspNetCore.Configuration;

[Trait("Feature", "Validator registration")]
public sealed class ValidatorConfigurationTests
{
    [Fact(DisplayName = "A request validator is registered as a request validator and not as a feature validator")]
    public void RequestValidatorsAreFoundOnlyAsRequestValidators()
    {
        // Arrange
        using var provider = Scan();

        // Act
        var asRequest = provider.GetServices<IRequestValidator<ScanRequest>>();
        var asFeature = provider.GetServices<IFeatureValidator<ScanRequest>>();

        // Assert
        Assert.Single(asRequest);
        Assert.Empty(asFeature);
    }

    [Fact(DisplayName = "A feature validator is registered as a feature validator and not as a request validator")]
    public void FeatureValidatorsAreFoundOnlyAsFeatureValidators()
    {
        // Arrange
        using var provider = Scan();

        // Act
        var asFeature = provider.GetServices<IFeatureValidator<ScanEntity>>();
        var asRequest = provider.GetServices<IRequestValidator<ScanEntity>>();

        // Assert
        Assert.Single(asFeature);
        Assert.Empty(asRequest);
    }

    [Fact(DisplayName = "A plain validator that derives from neither base is not registered, so nothing runs it by accident")]
    public void PlainValidatorsAreNotRegistered()
    {
        // Arrange
        using var provider = Scan();

        // Act
        var plain = provider.GetService<IValidator<ScanPlain>>();
        var asRequest = provider.GetService<IRequestValidator<ScanPlain>>();
        var asFeature = provider.GetService<IFeatureValidator<ScanPlain>>();

        // Assert
        Assert.Null(plain);
        Assert.Null(asRequest);
        Assert.Null(asFeature);
    }

    [Fact(DisplayName = "One request type can have a request validator and a feature validator at once, each found under its own marker")]
    public void SameRequestTypeCanHaveBothKindsOfValidator()
    {
        // Arrange
        using var provider = Scan();

        // Act
        var asRequest = provider.GetServices<IRequestValidator<ScanBoth>>().ToList();
        var asFeature = provider.GetServices<IFeatureValidator<ScanBoth>>().ToList();

        // Assert
        Assert.IsType<ScanBothShapeValidator>(Assert.Single(asRequest));
        Assert.IsType<ScanBothBusinessValidator>(Assert.Single(asFeature));
    }

    private static ServiceProvider Scan()
    {
        return new ServiceCollection()
            .AddAuroreValidators([typeof(ValidatorConfigurationTests).Assembly])
            .BuildServiceProvider();
    }
}

internal sealed record ScanRequest(string Name);

internal sealed record ScanEntity(string Status);

internal sealed record ScanPlain(string Value);

internal sealed record ScanBoth(string Name);

internal sealed class ScanBothShapeValidator : RequestValidator<ScanBoth>
{
    public ScanBothShapeValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

internal sealed class ScanBothBusinessValidator : FeatureValidator<ScanBoth>
{
    public ScanBothBusinessValidator()
    {
        RuleFor(x => x.Name).NotEqual("taken");
    }
}

internal sealed class ScanPlainValidator : AbstractValidator<ScanPlain>
{
    public ScanPlainValidator()
    {
        RuleFor(x => x.Value).NotEmpty();
    }
}

internal sealed class ScanRequestValidator : RequestValidator<ScanRequest>
{
    public ScanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

internal sealed class ScanEntityValidator : FeatureValidator<ScanEntity>
{
    public ScanEntityValidator()
    {
        RuleFor(x => x.Status).NotEmpty();
    }
}
