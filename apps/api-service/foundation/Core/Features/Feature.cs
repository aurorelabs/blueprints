using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Results;

namespace Aurore.Foundation.Core.Features;

/// <summary>
/// A feature that takes a request and returns a result without a value (a command). Write the logic in ExecuteAsync and
/// inject the feature itself where it is used; the decorators enabled for it run around ExecuteAsync inside HandleAsync.
/// Built by hand, without the registration, it runs the logic alone.
/// </summary>
/// <typeparam name="TRequest">The request of the feature.</typeparam>
public abstract class Feature<TRequest> : FeatureBase
    where TRequest : notnull
{
    /// <summary>Runs the feature with its decorators.</summary>
    public async ValueTask<Result> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (Pipeline is null)
            return await ExecuteAsync(request, cancellationToken);

        var context = new FeatureContext
        {
            FeatureType = GetType(),
            RequestType = typeof(TRequest),
            Request = request,
            Services = Pipeline.Services,
            CancellationToken = cancellationToken,
        };

        return await Pipeline.RunAsync(context, async () => await ExecuteAsync(request, cancellationToken));
    }

    /// <summary>The logic of the feature.</summary>
    protected abstract ValueTask<Result> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// A feature that takes a request and returns a value. See <see cref="Feature{TRequest}"/>.
/// </summary>
/// <typeparam name="TRequest">The request of the feature.</typeparam>
/// <typeparam name="TResponse">The value returned on success.</typeparam>
public abstract class Feature<TRequest, TResponse> : FeatureBase
    where TRequest : notnull
{
    /// <summary>Runs the feature with its decorators.</summary>
    public async ValueTask<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (Pipeline is null)
            return await ExecuteAsync(request, cancellationToken);

        var context = new FeatureContext
        {
            FeatureType = GetType(),
            RequestType = typeof(TRequest),
            Request = request,
            Services = Pipeline.Services,
            CancellationToken = cancellationToken,
        };

        var result = await Pipeline.RunAsync(context, async () => await ExecuteAsync(request, cancellationToken));

        // A decorator that answers on its own returns a plain failure; give it the type of this feature's result.
        return result as Result<TResponse> ?? Result<TResponse>.Failure(result.Error!);
    }

    /// <summary>The logic of the feature.</summary>
    protected abstract ValueTask<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
}
