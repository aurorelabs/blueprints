using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Handlers;

/// <summary>
/// Catch-all for failures nobody planned for. Expected outcomes (validation, not found, conflict) are not handled here:
/// they should be returned as results by the endpoints, not thrown.
///
///   caller left (request aborted)   499, logged at Debug: nothing failed
///   malformed request               the status the framework chose (400, 408, 413...), logged as a warning without a stack trace
///   anything else                   500 with a generic problem, logged as an error with the exception
///
/// The response never contains exception details.
/// </summary>
public sealed class UnexpectedExceptionHandler(ILogger<UnexpectedExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>The 499 status (used by nginx) for a caller that disconnected before the response was ready.</summary>
    public const int ClientClosedRequestStatusCode = 499;

    /// <summary>
    /// Writes the problem response for the exception. Always handles it, so nothing falls through to the framework's
    /// default page.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case OperationCanceledException when context.RequestAborted.IsCancellationRequested:
                logger.LogDebug("The caller closed the request before it completed.");
                context.Response.StatusCode = ClientClosedRequestStatusCode;

                return true;

            case BadHttpRequestException badRequest:
                logger.LogWarning("Malformed request rejected with {StatusCode}: {Reason}", badRequest.StatusCode, badRequest.Message);
                await WriteProblemAsync(context, badRequest.StatusCode, "The request could not be processed.");

                return true;

            default:
                logger.LogError(exception, "An unexpected error occurred.");
                await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");

                return true;
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        return TypedResults
            .Problem(statusCode: statusCode, title: ReasonPhrases.GetReasonPhrase(statusCode), detail: detail)
            .ExecuteAsync(context);
    }
}
