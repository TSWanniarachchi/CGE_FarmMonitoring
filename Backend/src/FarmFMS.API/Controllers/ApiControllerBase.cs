using FarmFMS.API.Common.Errors;
using FarmFMS.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

/*
 * Description  : Base class for Farm FMS API controllers (maps Application results to HTTP responses).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.API.Controllers;

/// <summary>
/// Base class every Farm FMS controller inherits.
/// Converts a failed <see cref="Result"/> into an RFC 7807 ProblemDetails response with the same
/// shape as the global exception handler (status, title, detail, code, traceId).
/// Controllers do not use try/catch.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Returns 200 OK with the value, or a ProblemDetails response for a failed result.
    /// </summary>
    protected ActionResult<T> OkOrProblem<T>(Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? Ok(result.Value) : ProblemFrom(result.Error);
    }

    /// <summary>
    /// Returns 204 No Content, or a ProblemDetails response for a failed result.
    /// </summary>
    protected ActionResult NoContentOrProblem(Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? NoContent() : ProblemFrom(result.Error);
    }

    /// <summary>
    /// Converts an error into a ProblemDetails response (ValidationProblemDetails when it carries field errors).
    /// </summary>
    protected ObjectResult ProblemFrom(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = ErrorStatusCodes.For(error.Type);

        ProblemDetails problem;
        if (error.Type == ErrorType.Validation && error.ValidationErrors.Count > 0)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (field, messages) in error.ValidationErrors)
            {
                foreach (var message in messages)
                    modelState.AddModelError(field, message);
            }

            problem = ProblemDetailsFactory.CreateValidationProblemDetails(
                HttpContext, modelState, statusCode, detail: error.Message);
        }
        else
        {
            problem = ProblemDetailsFactory.CreateProblemDetails(
                HttpContext, statusCode, ReasonPhrases.GetReasonPhrase(statusCode), detail: error.Message);
        }

        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = statusCode };
    }
}
