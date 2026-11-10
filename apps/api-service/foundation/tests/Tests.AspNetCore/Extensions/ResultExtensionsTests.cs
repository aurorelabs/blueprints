using Aurore.Foundation.AspNetCore.Extensions;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Results;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Tests.AspNetCore.Extensions;

[Trait("Feature", "Result to HTTP")]
public sealed class ResultExtensionsTests
{
    public static TheoryData<ErrorKind, int> KindToStatus => new()
    {
        { ErrorKind.Validation, StatusCodes.Status400BadRequest },
        { ErrorKind.NotFound, StatusCodes.Status404NotFound },
        { ErrorKind.Conflict, StatusCodes.Status409Conflict },
        { ErrorKind.Unauthorized, StatusCodes.Status401Unauthorized },
        { ErrorKind.Forbidden, StatusCodes.Status403Forbidden },
        { ErrorKind.Failure, StatusCodes.Status500InternalServerError },
        { ErrorKind.Unavailable, StatusCodes.Status503ServiceUnavailable },
    };

    [Theory(DisplayName = "Each error kind maps to its HTTP status")]
    [MemberData(nameof(KindToStatus))]
    public void KindMapsToStatus(ErrorKind kind, int expected)
    {
        // Arrange
        var error = new ApplicationError("x.y", "message", kind);

        // Act
        var problem = error.ToHttpResult();

        // Assert
        Assert.Equal(expected, problem.StatusCode);
        Assert.Equal(expected, problem.ProblemDetails.Status);
    }

    [Fact(DisplayName = "The problem carries the message as detail and the stable code, and no errors when there are no details")]
    public void ProblemCarriesMessageAndCode()
    {
        // Arrange
        var error = StandardErrors.NotFound;

        // Act
        var problem = error.ToHttpResult();

        // Assert
        Assert.Equal(error.Message, problem.ProblemDetails.Detail);
        Assert.Equal(error.Code, problem.ProblemDetails.Extensions["code"]);
        Assert.DoesNotContain("errors", problem.ProblemDetails.Extensions.Keys);
    }

    [Fact(DisplayName = "Field details appear under errors")]
    public void DetailsAppearUnderErrors()
    {
        // Arrange
        var details = new Dictionary<string, string[]> { ["items.0.name"] = ["Required"] };
        var error = StandardErrors.Validation.WithDetails(details);

        // Act
        var problem = error.ToHttpResult();

        // Assert
        Assert.Equal(details, problem.ProblemDetails.Extensions["errors"]);
    }

    [Fact(DisplayName = "A successful result with a value is a 200 Ok carrying the value")]
    public void SuccessIsOk()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var response = result.ToHttpResult();

        // Assert
        var ok = Assert.IsType<Ok<int>>(response.Result);
        Assert.Equal(5, ok.Value);
    }

    [Fact(DisplayName = "A failed result with a value type becomes the problem for its error")]
    public void FailureIsAProblem()
    {
        // Arrange
        Result<int> result = StandardErrors.Conflict;

        // Act
        var response = result.ToHttpResult();

        // Assert
        var problem = Assert.IsType<ProblemHttpResult>(response.Result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact(DisplayName = "The caller can build the success response, for example a 201 Created")]
    public void SuccessCanBeCustomized()
    {
        // Arrange
        var result = Result<int>.Success(7);

        // Act
        var response = result.ToHttpResult(id => TypedResults.Created($"/orders/{id}", id));

        // Assert
        var created = Assert.IsType<Created<int>>(response.Result);
        Assert.Equal("/orders/7", created.Location);
    }

    [Fact(DisplayName = "A failed result skips the custom success builder")]
    public void FailureSkipsTheSuccessBuilder()
    {
        // Arrange
        Result<int> result = StandardErrors.NotFound;
        var called = false;

        // Act
        var response = result.ToHttpResult(id =>
        {
            called = true;

            return TypedResults.Created($"/orders/{id}", id);
        });

        // Assert
        Assert.False(called);
        Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<ProblemHttpResult>(response.Result).StatusCode);
    }

    [Fact(DisplayName = "A result without a value is 204 on success")]
    public void PlainSuccessIsNoContent()
    {
        // Arrange
        var result = Result.Success;

        // Act
        var response = result.ToHttpResult();

        // Assert
        Assert.IsType<NoContent>(response.Result);
    }

    [Fact(DisplayName = "A result without a value becomes the problem for its error on failure")]
    public void PlainFailureIsAProblem()
    {
        // Arrange
        Result result = StandardErrors.Forbidden;

        // Act
        var response = result.ToHttpResult();

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, Assert.IsType<ProblemHttpResult>(response.Result).StatusCode);
    }

    [Fact(DisplayName = "A failed validation becomes a 400 problem with the field errors in one step")]
    public void ValidationBecomesAProblem()
    {
        // Arrange
        var validator = new InlineValidator<string>();
        validator.RuleFor(x => x).NotEmpty().WithMessage("Required");
        var validation = validator.Validate(string.Empty);

        // Act
        var problem = validation.ToHttpResult();

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);
        Assert.Equal("general.validation", problem.ProblemDetails.Extensions["code"]);
        Assert.Contains("errors", problem.ProblemDetails.Extensions.Keys);
    }

    [Fact(DisplayName = "A failed validation can use another error, such as a conflict, and keeps the field errors")]
    public void ValidationCanUseAnotherError()
    {
        // Arrange
        var validator = new InlineValidator<string>();
        validator.RuleFor(x => x).NotEmpty().WithMessage("Required");
        var validation = validator.Validate(string.Empty);
        var conflict = ApplicationError.Conflict("order.cannot_cancel", "Cannot cancel.");

        // Act
        var problem = validation.ToHttpResult(conflict);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Equal("order.cannot_cancel", problem.ProblemDetails.Extensions["code"]);
        Assert.Contains("errors", problem.ProblemDetails.Extensions.Keys);
    }
}
