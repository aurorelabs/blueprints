using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Validation;
using Microsoft.Extensions.DependencyInjection;
using Tests.Core.Features.Support;

namespace Tests.Core.Features.Decorators;

[Trait("Feature", "Feature validation")]
public sealed class ValidationDecoratorTests
{
    [Fact(DisplayName = "A request with no feature validator goes straight to the feature")]
    public async Task NoValidatorsRunsTheFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(features => features.WithValidation());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "A failed business rule stops the feature and answers with the error the validator declares")]
    public async Task FailedRuleAnswersWithTheDeclaredError()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            features => features.WithValidation(),
            services => services.AddScoped<IFeatureValidator<Ping>, TakenNameValidator>());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("taken"), CancellationToken.None);

        // Assert
        Assert.Equal(TakenNameValidator.Taken.Code, result.Error!.Code);
        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.Contains("name", result.Error.Details.Keys);
        Assert.Empty(provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "A validator that declares no error answers as a plain validation error")]
    public async Task ValidatorWithoutAnErrorAnswersAsValidation()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            features => features.WithValidation(),
            services => services.AddScoped<IFeatureValidator<Ping>, PlainBusinessValidator>());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("banned"), CancellationToken.None);

        // Assert
        Assert.Equal(ErrorKind.Validation, result.Error!.Kind);
        Assert.Contains("name", result.Error.Details.Keys);
    }

    [Fact(DisplayName = "A request that passes every validator reaches the feature")]
    public async Task ValidRequestReachesTheFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            features => features.WithValidation(),
            services => services
                .AddScoped<IFeatureValidator<Ping>, TakenNameValidator>()
                .AddScoped<IFeatureValidator<Ping>, PlainBusinessValidator>());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("ana"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(["feature"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact(DisplayName = "The validators of the request's shape are left to the HTTP filter, even for the same request type")]
    public async Task ShapeValidatorsAreNotRunByTheFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            features => features.WithValidation(),
            services => services
                .AddScoped<IRequestValidator<Ping>, ShapeValidator>()
                .AddScoped<IFeatureValidator<Ping>, PlainBusinessValidator>());

        // Act
        var result = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping(string.Empty), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "Validation can be enabled for one feature with the WithValidation attribute")]
    public async Task AttributeEnablesValidationForOneFeature()
    {
        // Arrange
        using var provider = FeatureTestHost.Provider(
            register: services => services.AddScoped<IFeatureValidator<Ping>, TakenNameValidator>());

        // Act
        var validated = await provider.GetRequiredService<ValidatedFeature>().HandleAsync(new Ping("taken"), CancellationToken.None);
        var plain = await provider.GetRequiredService<PlainFeature>().HandleAsync(new Ping("taken"), CancellationToken.None);

        // Assert
        Assert.True(validated.IsFailure);
        Assert.True(plain.IsSuccess);
    }
}
