using FarmFMS.Application.Common.Results;

/*
 * Description  : Service-level authentication errors (no SQL) (Step 0 spec section 5.4).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Errors raised by the authentication services. Messages are generic on purpose (no account enumeration, D-12).
/// </summary>
public static class AuthErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials",
        "Invalid username or password.");

    public static readonly Error PlatformAccessDenied = Error.Forbidden(
        "Auth.PlatformAccessDenied",
        "Your account does not have access to this application.");

    public static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "Auth.InvalidRefreshToken",
        "Your session is no longer valid. Please sign in again.");

    /// <summary>
    /// A protected request carries a session that is revoked, expired, idle or no longer allowed.
    /// Same code and message as the catalogue entry 50001 (<c>Auth.SessionInvalid</c>).
    /// </summary>
    public static readonly Error SessionInvalid = Error.Unauthorized(
        "Auth.SessionInvalid",
        "Your session is no longer valid. Please sign in again.");
}
