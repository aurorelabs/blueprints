using System.Collections.Generic;
using System.Linq;
using Aurore.Foundation.Core.Errors;
using FluentValidation.Results;

namespace Aurore.Foundation.Core.Validation;

/// <summary>Turns FluentValidation results into application errors.</summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Turns validation failures into an error, with the messages grouped by field in
    /// <see cref="ApplicationError.Details"/>. The error defines code, message and kind: the standard 400 validation error by
    /// default, or any other (for example a Conflict for a business rule checked with FluentValidation). Field paths are dotted
    /// camelCase with numeric indexes: "Items[0].Name"
    /// becomes "items.0.name", which is what form libraries use to find the field.
    /// </summary>
    public static ApplicationError ToApplicationError(this IEnumerable<ValidationFailure> failures, ApplicationError? error = null)
    {
        var details = failures
            .GroupBy(x => ToFieldPath(x.PropertyName))
            .ToDictionary(x => x.Key, x => x.Select(f => f.ErrorMessage).Distinct().ToArray());

        return (error ?? StandardErrors.Validation).WithDetails(details);
    }

    /// <summary>Converts a failed validation into an error; see the overload for failures.</summary>
    public static ApplicationError ToApplicationError(this ValidationResult result, ApplicationError? error = null)
    {
        return result.Errors.ToApplicationError(error);
    }

    internal static string ToFieldPath(string propertyName)
    {
        var segments = propertyName
            .Replace('[', '.')
            .Replace("]", string.Empty)
            .Split('.', System.StringSplitOptions.RemoveEmptyEntries)
            .Select(ToCamelCase);

        return string.Join('.', segments);
    }

    private static string ToCamelCase(string segment)
    {
        return char.IsAsciiLetterUpper(segment[0]) ? char.ToLowerInvariant(segment[0]) + segment[1..] : segment;
    }
}
