using FarmFMS.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

/*
 * Description  : Converts unhandled exceptions into RFC 7807 ProblemDetails responses.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.API.Common.Errors;

/// <summary>
/// Centralised exception handling. Controllers contain no try/catch.
/// <list type="bullet">
/// <item><see cref="DataAccessException"/> -> status from its error type, with its user-safe message.</item>
/// <item><see cref="BadHttpRequestException"/> -> its status code (e.g. 400, 413).</item>
/// <item>Request cancelled by the client -> 499, nothing written.</item>
/// <item>Anything else -> 500 with a generic message.</item>
/// </list>
/// Exception messages and stack traces are never sent to the client; the response carries a traceId
/// that matches the server log entry.
/// </summary>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService ??
            throw new ArgumentNullException(nameof(problemDetailsService));
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var request = httpContext.Request;

        // The client went away: nothing to report to it.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request {Method} {Path} was cancelled by the client", request.Method, request.Path);
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var (statusCode, detail, code) = exception switch
        {
            DataAccessException dataAccess => (ErrorStatusCodes.For(dataAccess.Error.Type), dataAccess.Error.Message, dataAccess.Error.Code),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request could not be processed.", "Request.Invalid"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.", "Server.Unexpected")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError && exception is not DataAccessException)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", request.Method, request.Path);
        }
        else
        {
            // DataAccessException details are already logged by the executor / unit of work.
            _logger.LogWarning("Request {Method} {Path} failed with {StatusCode} ({ErrorCode})",
                request.Method, request.Path, statusCode, code);
        }

        if (httpContext.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started; the error response cannot be written");
            return false;
        }

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail
        };
        problemDetails.Extensions["code"] = code;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
