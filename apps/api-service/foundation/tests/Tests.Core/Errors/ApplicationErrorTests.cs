using Aurore.Foundation.Core.Errors;

namespace Tests.Core.Errors;

[Trait("Feature", "Errors")]
public sealed class ApplicationErrorTests
{
    public static TheoryData<ErrorKind> AllKinds
    {
        get
        {
            var kinds = new TheoryData<ErrorKind>();
            kinds.AddRange(Enum.GetValues<ErrorKind>());

            return kinds;
        }
    }

    [Theory(DisplayName = "Every kind has a factory that creates an error of that kind with the given code and message")]
    [MemberData(nameof(AllKinds))]
    public void FactoryCreatesTheMatchingKind(ErrorKind kind)
    {
        // Arrange
        // Act
        var error = Create(kind, "area.some_error", "Something happened.");

        // Assert
        Assert.Equal(kind, error.Kind);
        Assert.Equal("area.some_error", error.Code);
        Assert.Equal("Something happened.", error.Message);
    }

    [Fact(DisplayName = "WithDetails returns a copy with the details and leaves the shared error untouched")]
    public void WithDetailsDoesNotMutateTheOriginal()
    {
        // Arrange
        var shared = ApplicationError.Conflict("order.cannot_cancel", "Cannot cancel.");
        var details = new Dictionary<string, string[]> { ["status"] = ["Only pending orders can be cancelled."] };

        // Act
        var copy = shared.WithDetails(details);

        // Assert
        Assert.Empty(shared.Details);
        Assert.Equal(details, copy.Details);
        Assert.Equal(shared.Code, copy.Code);
        Assert.Equal(shared.Kind, copy.Kind);
    }

    private static ApplicationError Create(ErrorKind kind, string code, string message)
    {
        return kind switch
        {
            ErrorKind.Validation => ApplicationError.Validation(code, message),
            ErrorKind.NotFound => ApplicationError.NotFound(code, message),
            ErrorKind.Conflict => ApplicationError.Conflict(code, message),
            ErrorKind.Unauthorized => ApplicationError.Unauthorized(code, message),
            ErrorKind.Forbidden => ApplicationError.Forbidden(code, message),
            ErrorKind.Failure => ApplicationError.Failure(code, message),
            ErrorKind.Unavailable => ApplicationError.Unavailable(code, message),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "This kind has no factory."),
        };
    }
}
