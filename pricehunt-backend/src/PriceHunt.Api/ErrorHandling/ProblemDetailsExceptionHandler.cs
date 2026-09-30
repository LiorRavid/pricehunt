using Microsoft.AspNetCore.Diagnostics;

namespace PriceHunt.Api.ErrorHandling;

/// <summary>
/// Turns unhandled exceptions into RFC 9457 problem details. Bad requests keep their status
/// and message; anything else becomes a 500 without internal details.
/// </summary>
internal sealed partial class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int statusCode, string title, string? detail) = exception switch
        {
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request is invalid.", badRequest.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null),
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, httpContext.Request.Method, httpContext.Request.Path, exception);
        }

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, string method, PathString path, Exception exception);
}
