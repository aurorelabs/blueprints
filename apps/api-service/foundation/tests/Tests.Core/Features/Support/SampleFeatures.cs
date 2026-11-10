using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Core.Validation;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Features.Attributes;
using FluentValidation;

namespace Tests.Core.Features.Support;

public sealed record Ping(string Name);

/// <summary>What happened during a run, in order. Registered as a singleton so tests can read it afterwards.</summary>
public sealed class Trace
{
    public List<string> Steps { get; } = [];
}

public sealed class PlainFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success($"hello {request.Name}"));
    }
}

public sealed class CommandFeature(Trace trace) : Feature<Ping>
{
    protected override ValueTask<Result> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result.Success);
    }
}

[With<FirstDecorator>]
public sealed class FirstFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success("ok"));
    }
}

[Without<FirstDecorator>]
public sealed class QuietFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success("ok"));
    }
}

[With<FirstDecorator>]
[Without<FirstDecorator>]
public sealed class ContradictoryFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success("ok"));
    }
}

[WithValidation]
public sealed class ValidatedFeature(Trace trace) : Feature<Ping, string>
{
    protected override ValueTask<Result<string>> ExecuteAsync(Ping request, CancellationToken cancellationToken)
    {
        trace.Steps.Add("feature");

        return ValueTask.FromResult(Result<string>.Success("ok"));
    }
}

public abstract class NotAFeature;

public abstract class AbstractFeature : Feature<Ping, string>;

public sealed class FirstDecorator(Trace trace) : IFeatureDecorator
{
    public int Order => 100;

    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        trace.Steps.Add("first:before");
        var result = await next();
        trace.Steps.Add("first:after");

        return result;
    }
}

public sealed class SecondDecorator(Trace trace) : IFeatureDecorator
{
    public int Order => 200;

    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        trace.Steps.Add("second:before");
        var result = await next();
        trace.Steps.Add("second:after");

        return result;
    }
}

/// <summary>A custom decorator that does not set an Order, so it takes the default and runs last.</summary>
public sealed class DefaultOrderDecorator(Trace trace) : IFeatureDecorator
{
    public async ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        trace.Steps.Add("default:before");

        return await next();
    }
}

/// <summary>Answers on its own, without calling the feature.</summary>
public sealed class RefusingDecorator : IFeatureDecorator
{
    public static readonly ApplicationError Refused = ApplicationError.Forbidden("test.refused", "Refused.");

    public ValueTask<Result> InvokeAsync(FeatureContext context, FeatureDelegate next)
    {
        return ValueTask.FromResult<Result>(Refused);
    }
}

public sealed class TakenNameValidator : FeatureValidator<Ping>
{
    public static readonly ApplicationError Taken = ApplicationError.Conflict("test.taken", "The name is taken.");

    public TakenNameValidator()
    {
        RuleFor(x => x.Name).NotEqual("taken").WithMessage("The name is taken.");
    }

    public override ApplicationError? Error => Taken;
}

public sealed class PlainBusinessValidator : FeatureValidator<Ping>
{
    public PlainBusinessValidator()
    {
        RuleFor(x => x.Name).NotEqual("banned").WithMessage("The name is banned.");
    }
}

public sealed class ShapeValidator : RequestValidator<Ping>
{
    public ShapeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("The name is required.");
    }
}
