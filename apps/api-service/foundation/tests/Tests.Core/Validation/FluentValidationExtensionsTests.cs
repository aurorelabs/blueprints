using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Validation;
using FluentValidation;
using FluentValidation.Results;

namespace Tests.Core.Validation;

[Trait("Feature", "Validation")]
public sealed class FluentValidationExtensionsTests
{
    private static readonly InlineValidator<Order> _validator = CreateValidator();

    [Fact(DisplayName = "Failures become the standard validation error with the messages grouped by field")]
    public void FailuresBecomeTheStandardValidationError()
    {
        // Arrange
        var result = Validate(new Order("", new Shipping(5), [new Item("ok")]));

        // Act
        var error = result.ToApplicationError();

        // Assert
        Assert.Equal(StandardErrors.Validation.Code, error.Code);
        Assert.Equal(ErrorKind.Validation, error.Kind);
        Assert.Equal(["Email is required"], error.Details["email"]);
    }

    [Fact(DisplayName = "Field paths are dotted camelCase with numeric indexes")]
    public void FieldPathsAreDottedCamelCase()
    {
        // Arrange
        var result = Validate(new Order("a@b.c", new Shipping(-1), [new Item("ok"), new Item("")]));

        // Act
        var error = result.ToApplicationError();

        // Assert
        Assert.Equal(["Must be positive"], error.Details["shipping.cost"]);
        Assert.Equal(["Name is required"], error.Details["items.1.name"]);
        Assert.DoesNotContain("items.0.name", error.Details.Keys);
    }

    [Fact(DisplayName = "Several messages for one field are kept together, without duplicates")]
    public void MessagesAreGroupedAndDistinct()
    {
        // Arrange
        var validator = new InlineValidator<Order>();
        validator.RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
        validator.RuleFor(x => x.Email).MinimumLength(3).WithMessage("Email is too short");
        validator.RuleFor(x => x.Email).MinimumLength(3).WithMessage("Email is too short");
        var result = validator.Validate(new Order("", new Shipping(1), []));

        // Act
        var error = result.ToApplicationError();

        // Assert
        Assert.Equal(["Email is required", "Email is too short"], error.Details["email"]);
    }

    [Fact(DisplayName = "Another error can define the code, message and kind while the field details are attached")]
    public void AnotherErrorKeepsItsKindAndGetsTheDetails()
    {
        // Arrange
        var conflict = ApplicationError.Conflict("order.cannot_cancel", "The order can no longer be cancelled.");
        var result = Validate(new Order("", new Shipping(1), []));

        // Act
        var error = result.ToApplicationError(conflict);

        // Assert
        Assert.Equal("order.cannot_cancel", error.Code);
        Assert.Equal(ErrorKind.Conflict, error.Kind);
        Assert.Contains("email", error.Details.Keys);
        Assert.Empty(conflict.Details);
    }

    private static ValidationResult Validate(Order order)
    {
        return _validator.Validate(order);
    }

    private static InlineValidator<Order> CreateValidator()
    {
        var validator = new InlineValidator<Order>();

        validator.RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
        validator.RuleFor(x => x.Shipping.Cost).GreaterThan(0).WithMessage("Must be positive");
        validator.RuleForEach(x => x.Items).ChildRules(item => item.RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required"));

        return validator;
    }

    private sealed record Item(string Name);

    private sealed record Shipping(decimal Cost);

    private sealed record Order(string Email, Shipping Shipping, Item[] Items);
}
