/*
 * Description  : Defines the categories of application errors.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Common.Results;

/// <summary>
/// Category of an <see cref="Error"/>. The API maps each category to one HTTP status code.
/// </summary>
public enum ErrorType
{
    /// <summary>Unexpected failure (HTTP 500).</summary>
    Failure = 0,

    /// <summary>Invalid input (HTTP 400).</summary>
    Validation = 1,

    /// <summary>The requested record does not exist (HTTP 404).</summary>
    NotFound = 2,

    /// <summary>The operation conflicts with existing data, e.g. a duplicate value (HTTP 409).</summary>
    Conflict = 3,

    /// <summary>The caller is not signed in (HTTP 401).</summary>
    Unauthorized = 4,

    /// <summary>The caller is signed in but not allowed to do this (HTTP 403).</summary>
    Forbidden = 5,

    /// <summary>A temporary condition (timeout, deadlock, database unavailable); the caller may retry (HTTP 503).</summary>
    Unavailable = 6
}
