using System;
using System.Collections.Generic;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.Core.Validation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aurore.Foundation.AspNetCore.Extensions;

/// <summary>
/// Turns results and errors into HTTP responses. The return types are typed unions (Results&lt;Ok&lt;T&gt;, ProblemHttpResult&gt;),
/// so OpenAPI describes the endpoint's responses without any extra metadata.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// A failed result becomes a problem response; success is 200 with the value.
    /// </summary>
    public static Results<Ok<T>, ProblemHttpResult> ToHttpResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : result.Error!.ToHttpResult();
    }

    /// <summary>
    /// A failed result becomes a problem response; success is built by <paramref name="onSuccess"/>, for example
    /// <c>value => TypedResults.Created($"/orders/{value.Id}", value)</c>.
    /// </summary>
    public static Results<TSuccess, ProblemHttpResult> ToHttpResult<T, TSuccess>(this Result<T> result, Func<T, TSuccess> onSuccess)
        where TSuccess : IResult
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : result.Error!.ToHttpResult();
    }

    /// <summary>
    /// A failed result becomes a problem response; success is 204 No Content.
    /// </summary>
    public static Results<NoContent, ProblemHttpResult> ToHttpResult(this Result result)
    {
        return result.IsSuccess
            ? TypedResults.NoContent()
            : result.Error!.ToHttpResult();
    }

    /// <summary>
    /// The problem response for an error: status from its kind, "detail" is the message, "code" is the stable code, and
    /// "errors" holds the field details when there are any.
    /// </summary>
    public static ProblemHttpResult ToHttpResult(this ApplicationError error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error.Details.Count > 0)
        {
            extensions["errors"] = error.Details;
        }

        return TypedResults.Problem(detail: error.Message, statusCode: ToStatusCode(error.Kind), extensions: extensions);
    }

    /// <summary>
    /// Failed FluentValidation results become a problem response in one step. By default it is the 400 validation error; pass
    /// another error to change the code, message and kind (for example a Conflict for a business rule). The field details are
    /// attached either way.
    /// </summary>
    public static ProblemHttpResult ToHttpResult(this ValidationResult validation, ApplicationError? error = null)
    {
        return validation.ToApplicationError(error).ToHttpResult();
    }

    internal static int ToStatusCode(ErrorKind kind)
    {
        return kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
