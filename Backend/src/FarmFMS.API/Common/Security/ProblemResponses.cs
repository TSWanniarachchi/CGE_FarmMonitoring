using Microsoft.AspNetCore.Mvc;

/*
 * Description  : Writes generic ProblemDetails responses from middleware and authentication events.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.API.Common.Security;

/// <summary>
/// Middleware-level errors (401, 403, 429) use the same ProblemDetails shape as controller errors (status, title,
/// detail, code, traceId). The text is always generic: never an exception message or a validation reason.
/// </summary>
internal static class ProblemResponses
{
    public const string Unauthenticated = "Auth.Unauthenticated";
    public const string SessionInvalid = "Auth.SessionInvalid";
    public const string Forbidden = "Auth.Forbidden";
    public const string TooManyRequests = "RateLimit.Exceeded";

    public static async Task WriteAsync(HttpContext context, int statusCode, string title, string detail, string code)
    {
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails { Status = statusCode, Title = title, Detail = detail };
        problem.Extensions["code"] = code;

        var service = context.RequestServices.GetService<IProblemDetailsService>();
        if (service is null ||
            !await service.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem }))
        {
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}
