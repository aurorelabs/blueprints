using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Handlers;

public sealed class UnexpectedExceptionHandler(ILogger<UnexpectedExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unexpected error occurred.");

        await TypedResults
            .Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Internal Server Error", detail: "An unexpected error occurred.")
            .ExecuteAsync(context);

        return true;
    }
}
