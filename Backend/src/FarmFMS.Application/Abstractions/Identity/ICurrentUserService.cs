/*
 * Description  : Defines access to the user making the current request (used for audit fields).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Abstractions.Identity;

/// <summary>
/// Provides the user making the current request.
/// Services use it to set CreatedBy / ModifiedBy (INT user IDs in the Module 01 specification);
/// clients never send audit fields.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// True when the request has an authenticated user.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// The signed-in user's ID, or null when the request is not authenticated
    /// or carries no valid numeric user ID claim.
    /// </summary>
    int? UserId { get; }

    /// <summary>
    /// The session ID (<c>UserSession.Id</c>, the "sid" claim) behind the access token, or null when the request is
    /// not authenticated or carries no valid numeric session ID claim.
    /// </summary>
    int? SessionId { get; }

    /// <summary>
    /// The signed-in user's name, or null when the request is not authenticated.
    /// </summary>
    string? UserName { get; }
}
