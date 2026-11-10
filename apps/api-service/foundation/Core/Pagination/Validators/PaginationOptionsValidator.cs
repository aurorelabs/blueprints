using Aurore.Foundation.Core.Pagination.Models;
using FluentValidation;

namespace Aurore.Foundation.Core.Pagination.Validators;

/// <summary>Checks the pagination settings.</summary>
public sealed class PaginationOptionsValidator : AbstractValidator<PaginationOptions>
{
    /// <summary>Creates the validator.</summary>
    public PaginationOptionsValidator()
    {
        RuleFor(x => x.DefaultPageSize).GreaterThan(0);
        RuleFor(x => x.MaxPageSize).GreaterThan(0);
        RuleFor(x => x.DefaultPageSize).LessThanOrEqualTo(x => x.MaxPageSize)
            .When(x => x.DefaultPageSize > 0 && x.MaxPageSize > 0)
            .WithMessage("DefaultPageSize must not be greater than MaxPageSize.");
    }
}
