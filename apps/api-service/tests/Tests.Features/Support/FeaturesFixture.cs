using System;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Domain;
using Aurore.Blueprints.ApiService.Features;
using Aurore.Blueprints.ApiService.Features.Authors;
using Aurore.Blueprints.ApiService.Features.Configuration;
using Aurore.Blueprints.ApiService.Features.Posts;
using Aurore.Blueprints.ApiService.Infra.Configuration;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.AspNetCore.Configuration;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Tests.Support;

namespace Tests.Features.Support;

/// <summary>
/// The features wired like the application wires them (decorators, validators, database), on a database of their own that has the
/// migrations applied. The time is a fake one that tests can move. Every test makes its own authors and posts with unique names,
/// so tests of one class do not see each other's data.
/// </summary>
public sealed class FeaturesFixture : IAsyncLifetime
{
    private TestDatabase? _database;

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 11, 11, 10, 0, 0, TimeSpan.Zero));

    public ServiceProvider Provider { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync(TestContext.Current.CancellationToken);

        Provider = new ServiceCollection()
            .AddLogging()
            .AddSingleton<TimeProvider>(Time)
            .AddSingleton(new PaginationOptions { DefaultPageSize = 10, MaxPageSize = 50 })
            .AddAuroreValidators([typeof(IFeatures).Assembly])
            .AddAppFeatures()
            .AddAppDatabase(_database.Options)
            .BuildServiceProvider();
    }

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();

        if (_database is not null)
            await _database.DisposeAsync();
    }

    /// <summary>Runs a feature that returns a value, in a scope of its own like a request.</summary>
    public async Task<Result<TResponse>> RunAsync<TFeature, TRequest, TResponse>(TRequest request)
        where TFeature : Feature<TRequest, TResponse>
        where TRequest : notnull
    {
        await using var scope = Provider.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<TFeature>().HandleAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Runs a feature that returns no value, in a scope of its own like a request.</summary>
    public async Task<Result> RunAsync<TFeature, TRequest>(TRequest request)
        where TFeature : Feature<TRequest>
        where TRequest : notnull
    {
        await using var scope = Provider.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<TFeature>().HandleAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Reads the database directly, for what the features do not expose (deleted rows, for one).</summary>
    public async Task<T> QueryAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        await using var scope = Provider.CreateAsyncScope();

        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    /// <summary>A new author with a subject and a handle nobody else has.</summary>
    public async Task<Author> NewAuthorAsync(string name = "writer")
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var result = await RunAsync<UpsertAuthor, UpsertAuthorRequest, AuthorResponse>(new UpsertAuthorRequest($"subject-{unique}", $"{name}-{unique}", $"Writer {unique}"));

        return new Author(new Caller($"subject-{unique}", IsAdmin: false), result.Value);
    }

    /// <summary>A new post by the author, a draft unless asked to be published.</summary>
    public async Task<PostResponse> NewPostAsync(Author author, bool publish = false, string? title = null)
    {
        var slug = $"post-{Guid.NewGuid():N}";
        var created = await RunAsync<CreatePost, CreatePostRequest, PostResponse>(new CreatePostRequest(author.Caller.Subject, slug, title ?? $"Title of {slug}", "The text."));

        if (publish is false)
            return created.Value;

        return (await RunAsync<PublishPost, PublishPostRequest, PostResponse>(new PublishPostRequest(created.Value.Id, author.Caller))).Value;
    }

    /// <summary>An author the tests can act as: who is calling, and the profile that was created for it.</summary>
    public sealed record Author(Caller Caller, AuthorResponse Profile);
}
