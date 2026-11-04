using FarmFMS.Application.Common.Results;

/*
 * Description  : Defines the errors returned when the audit actor cannot be resolved.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Common.Identity;

public static class AuditErrors
{
    /// <summary>
    /// A write was attempted without an authenticated user and outside a system operation.
    /// </summary>
    public static readonly Error ActorRequired = Error.Unauthorized(
        "Audit.ActorRequired",
        "You must be signed in to perform this operation.");

    /// <summary>
    /// A system operation ran, but the System user ID is not configured for this environment.
    /// </summary>
    public static readonly Error SystemUserNotConfigured = Error.Failure(
        "Audit.SystemUserNotConfigured",
        "The operation could not be completed. Please contact the administrator.");
}
