using System.Reflection;
using Aurore.Foundation.AspNetCore.RequestPipeline.Filters;
using Aurore.Foundation.Core.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Tests.AspNetCore.Support;

namespace Tests.AspNetCore.RequestPipeline.Filters;

[Trait("Feature", "Request validation")]
public sealed class RequestValidationFilterTests
{
    [Fact(DisplayName = "An endpoint with no validated parameter gets no filter at all")]
    public void EndpointWithoutValidatorsIsLeftUntouched()
    {
        // Arrange
        var provider = Provider(services => services.AddSingleton<IRequestValidator<Widget>, WidgetNameValidator>());
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>("ok");

        // Act
        var filter = RequestValidationFilter.Create(Factory(nameof(PrimitivesOnly), provider), next);

        // Assert
        Assert.Same(next, filter);
    }

    [Fact(DisplayName = "Business validators are never run by the filter, so they cannot turn a business rule into a 400")]
    public void FeatureValidatorsAreIgnored()
    {
        // Arrange
        var provider = Provider(services => services
            .AddSingleton<IValidator<Widget>, WidgetBusinessValidator>()
            .AddSingleton<IFeatureValidator<Widget>, WidgetBusinessValidator>());
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>("ok");

        // Act
        var filter = RequestValidationFilter.Create(Factory(nameof(WidgetOnly), provider), next);

        // Assert
        Assert.Same(next, filter);
    }

    [Fact(DisplayName = "A valid argument reaches the handler")]
    public async Task ValidArgumentCallsNext()
    {
        // Arrange
        var provider = Provider(services => services.AddSingleton<IRequestValidator<Widget>, WidgetNameValidator>());

        // Act
        var run = await Run(provider, nameof(WidgetWithPrimitives), new Widget("gadget"), 7, CancellationToken.None);

        // Assert
        Assert.True(run.NextCalled);
        Assert.Equal("ok", run.Result);
    }

    [Fact(DisplayName = "An invalid argument is answered with the 400 validation problem and never reaches the handler")]
    public async Task InvalidArgumentReturnsTheValidationProblem()
    {
        // Arrange
        var provider = Provider(services => services.AddSingleton<IRequestValidator<Widget>, WidgetNameValidator>());

        // Act
        var run = await Run(provider, nameof(WidgetWithPrimitives), new Widget(string.Empty), 7, CancellationToken.None);

        // Assert
        var problem = Assert.IsType<ProblemHttpResult>(run.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Contains("name", Assert.IsType<Dictionary<string, string[]>>(problem.ProblemDetails.Extensions["errors"]).Keys);
        Assert.False(run.NextCalled);
    }

    [Fact(DisplayName = "A missing (null) argument is skipped, leaving the framework's own binding rules in charge")]
    public async Task NullArgumentIsSkipped()
    {
        // Arrange
        var provider = Provider(services => services.AddSingleton<IRequestValidator<Widget>, WidgetNameValidator>());

        // Act
        var run = await Run(provider, nameof(WidgetWithPrimitives), null, 7, CancellationToken.None);

        // Assert
        Assert.True(run.NextCalled);
    }

    [Fact(DisplayName = "Every validator registered for a type runs, so a second one is never ignored")]
    public async Task EveryValidatorOfATypeRuns()
    {
        // Arrange
        var provider = Provider(services => services
            .AddSingleton<IRequestValidator<Widget>, WidgetLengthValidator>()
            .AddSingleton<IRequestValidator<Widget>, WidgetPrefixValidator>());

        // Act
        var run = await Run(provider, nameof(WidgetOnly), new Widget("abcd"));

        // Assert
        var errors = ErrorsOf(run.Result);
        Assert.Contains("Name is too long", errors["name"]);
        Assert.Contains("Name must start with x", errors["name"]);
    }

    [Fact(DisplayName = "Every validated argument is checked and their errors come back together")]
    public async Task ErrorsOfAllArgumentsAreCombined()
    {
        // Arrange
        var provider = Provider(services => services
            .AddSingleton<IRequestValidator<Widget>, WidgetNameValidator>()
            .AddSingleton<IRequestValidator<Paging>, PagingValidator>());

        // Act
        var run = await Run(provider, nameof(BodyAndQuery), new Widget(string.Empty), new Paging(0));

        // Assert
        var errors = ErrorsOf(run.Result);
        Assert.Contains("name", errors.Keys);
        Assert.Contains("page", errors.Keys);
    }

    [Fact(DisplayName = "The validator is chosen by the declared parameter type, not by the type of the object received")]
    public async Task ValidatorIsChosenByTheDeclaredType()
    {
        // Arrange
        var provider = Provider(services => services.AddSingleton<IRequestValidator<IShape>, ShapeValidator>());

        // Act
        var run = await Run(provider, nameof(ShapeOnly), new Circle(string.Empty));

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsType<ProblemHttpResult>(run.Result).StatusCode);
        Assert.False(run.NextCalled);
    }

    private static ServiceProvider Provider(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection().AddLogging();
        register(services);

        return services.BuildServiceProvider();
    }

    private static EndpointFilterFactoryContext Factory(string handlerName, IServiceProvider provider)
    {
        var handler = typeof(RequestValidationFilterTests).GetMethod(handlerName, BindingFlags.NonPublic | BindingFlags.Static)!;

        return new EndpointFilterFactoryContext { MethodInfo = handler, ApplicationServices = provider };
    }

    private static async Task<RunResult> Run(ServiceProvider provider, string handlerName, params object?[] arguments)
    {
        var nextCalled = false;
        var filter = RequestValidationFilter.Create(
            Factory(handlerName, provider),
            _ =>
            {
                nextCalled = true;

                return ValueTask.FromResult<object?>("ok");
            });
        var http = HttpContextFactory.Create(provider);

        var result = await filter(new DefaultEndpointFilterInvocationContext(http, arguments));

        return new RunResult(result, nextCalled);
    }

    private static Dictionary<string, string[]> ErrorsOf(object? result)
    {
        var problem = Assert.IsType<ProblemHttpResult>(result);

        return Assert.IsType<Dictionary<string, string[]>>(problem.ProblemDetails.Extensions["errors"]);
    }

    private static void PrimitivesOnly(int id, string name)
    {
    }

    private static void WidgetWithPrimitives(Widget widget, int id, CancellationToken cancellationToken)
    {
    }

    private static void WidgetOnly(Widget widget)
    {
    }

    private static void BodyAndQuery(Widget body, Paging query)
    {
    }

    private static void ShapeOnly(IShape shape)
    {
    }

    private sealed record RunResult(object? Result, bool NextCalled);

    private sealed record Widget(string Name);

    private sealed record Paging(int Page);

    private interface IShape
    {
        public string Name { get; }
    }

    private sealed record Circle(string Name) : IShape;

    private sealed class WidgetNameValidator : RequestValidator<Widget>
    {
        public WidgetNameValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }

    private sealed class WidgetBusinessValidator : FeatureValidator<Widget>
    {
        public WidgetBusinessValidator()
        {
            RuleFor(x => x.Name).Must(_ => false).WithMessage("Always fails");
        }
    }

    private sealed class WidgetLengthValidator : RequestValidator<Widget>
    {
        public WidgetLengthValidator()
        {
            RuleFor(x => x.Name).MaximumLength(3).WithMessage("Name is too long");
        }
    }

    private sealed class WidgetPrefixValidator : RequestValidator<Widget>
    {
        public WidgetPrefixValidator()
        {
            RuleFor(x => x.Name).Must(name => name.StartsWith('x')).WithMessage("Name must start with x");
        }
    }

    private sealed class PagingValidator : RequestValidator<Paging>
    {
        public PagingValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Page must be positive");
        }
    }

    private sealed class ShapeValidator : RequestValidator<IShape>
    {
        public ShapeValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }
}
