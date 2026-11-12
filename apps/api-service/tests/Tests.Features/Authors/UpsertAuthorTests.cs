using System;
using System.Linq;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Core.Errors;
using Aurore.Blueprints.ApiService.Features.Authors;
using Microsoft.EntityFrameworkCore;
using Tests.Features.Support;

namespace Tests.Features.Authors;

[Trait("Feature", "Author profile")]
public sealed class UpsertAuthorTests(FeaturesFixture features) : IClassFixture<FeaturesFixture>
{
    [Fact(DisplayName = "A caller without a profile gets one")]
    public async Task ProfileIsCreated()
    {
        // Arrange
        var unique = Guid.NewGuid().ToString("N")[..8];

        // Act
        var result = await features.RunAsync<UpsertAuthor, UpsertAuthorRequest, AuthorResponse>(new UpsertAuthorRequest($"subject-{unique}", $"ada-{unique}", "Ada Lovelace"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal($"ada-{unique}", result.Value.Handle);
        Assert.Equal("Ada Lovelace", result.Value.DisplayName);
        Assert.Equal(features.Time.GetUtcNow(), result.Value.CreatedAt);
    }

    [Fact(DisplayName = "Calling again changes the display name and keeps the same profile")]
    public async Task SecondCallUpdatesTheDisplayName()
    {
        // Arrange
        var author = await features.NewAuthorAsync();

        // Act
        var result = await features.RunAsync<UpsertAuthor, UpsertAuthorRequest, AuthorResponse>(new UpsertAuthorRequest(author.Caller.Subject, author.Profile.Handle, "New Name"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(author.Profile.Id, result.Value.Id);
        Assert.Equal("New Name", result.Value.DisplayName);
        Assert.Equal(1, await features.QueryAsync(db => db.Authors.CountAsync(x => x.Subject == author.Caller.Subject)));
    }

    [Fact(DisplayName = "The handle of an existing profile cannot change")]
    public async Task HandleCannotChange()
    {
        // Arrange
        var author = await features.NewAuthorAsync();

        // Act
        var result = await features.RunAsync<UpsertAuthor, UpsertAuthorRequest, AuthorResponse>(new UpsertAuthorRequest(author.Caller.Subject, "another-handle", "Name"));

        // Assert
        Assert.Equal(AppErrors.Authors.HandleCannotChange, result.Error);
    }

    [Fact(DisplayName = "A handle that belongs to another author is a conflict, whoever asks")]
    public async Task HandleTakenByAnotherAuthor()
    {
        // Arrange
        var owner = await features.NewAuthorAsync();

        // Act
        var result = await features.RunAsync<UpsertAuthor, UpsertAuthorRequest, AuthorResponse>(new UpsertAuthorRequest($"subject-{Guid.NewGuid():N}", owner.Profile.Handle, "Impostor"));

        // Assert
        Assert.Equal(AppErrors.Authors.HandleTaken, result.Error);
    }

    [Fact(DisplayName = "Two profiles with the same handle never exist, even if both are created at the same moment")]
    public async Task SameHandleAtTheSameMomentCreatesOneProfile()
    {
        // Arrange
        var handle = $"racing-{Guid.NewGuid().ToString("N")[..8]}";

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(i =>
            features.RunAsync<UpsertAuthor, UpsertAuthorRequest, AuthorResponse>(new UpsertAuthorRequest($"subject-{Guid.NewGuid():N}", handle, $"Racer {i}"))));

        // Assert
        Assert.Single(results, x => x.IsSuccess);
        Assert.All(results.Where(x => x.IsFailure), x => Assert.Equal(AppErrors.Authors.HandleTaken, x.Error));
    }
}
