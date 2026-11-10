using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Results;

namespace Tests.Core.Results;

[Trait("Feature", "Results")]
public sealed class ResultTests
{
    [Fact(DisplayName = "A value converts to a successful result carrying it")]
    public void ValueConvertsToSuccess()
    {
        // Arrange
        // Act
        Result<int> result = 42;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Equal(42, result.Value);
    }

    [Fact(DisplayName = "An error converts to a failed result carrying that same error")]
    public void ErrorConvertsToFailure()
    {
        // Arrange
        var error = StandardErrors.NotFound;

        // Act
        Result<int> result = error;

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
    }

    [Fact(DisplayName = "Reading the value of a failed result throws and names the error code")]
    public void ReadingValueOfFailureThrows()
    {
        // Arrange
        Result<string> result = StandardErrors.Conflict;

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);

        // Assert
        Assert.Contains(StandardErrors.Conflict.Code, exception.Message);
    }

    [Fact(DisplayName = "A result without a value succeeds through Success and fails through an error")]
    public void ResultWithoutValue()
    {
        // Arrange
        // Act
        Result failure = StandardErrors.Forbidden;

        // Assert
        Assert.True(Result.Success.IsSuccess);
        Assert.Null(Result.Success.Error);
        Assert.True(failure.IsFailure);
        Assert.Same(StandardErrors.Forbidden, failure.Error);
    }
}
