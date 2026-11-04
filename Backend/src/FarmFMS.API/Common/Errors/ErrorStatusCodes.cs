using FarmFMS.Application.Common.Results;

/*
 * Description  : Maps application error categories to HTTP status codes.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.API.Common.Errors;

/// <summary>
/// The single mapping from <see cref="ErrorType"/> to HTTP status code, used by controllers and the global exception handler.
/// </summary>
public static class ErrorStatusCodes
{
    public static int For(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };
}
