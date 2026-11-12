/*
 * Description  : User session repository contract and read models (N1, N4, N5, N6, SupUserSessRev).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Session data access. One method per Stored Procedure (spec section 5).
/// </summary>
public interface IUserSessionRepository
{
    /// <summary><c>dbo.ScrUsrSess</c> (N1): creates a session. Raises business error 50002 when the user is missing, inactive or locked.</summary>
    Task<CreatedSessionReadModel> CreateAsync(
        int userId,
        byte[] tokenHash,
        int absoluteLifetimeMinutes,
        string? ipAddress,
        string? deviceInfo,
        int createdBy,
        CancellationToken cancellationToken = default);

    /// <summary><c>dbo.SreUsrSessByTokenHash</c> (N4): the session of a refresh-token hash, or <c>null</c>.</summary>
    Task<SessionReadModel?> GetByTokenHashAsync(
        byte[] tokenHash,
        int idleTimeoutMinutes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>dbo.ScrUsrSessRotate</c> (N5): returns the new session Id. Raises business error 50001 when the current
    /// session is no longer active (concurrent use is treated as reuse).
    /// </summary>
    Task<int> RotateAsync(
        int currentSessionId,
        byte[] newTokenHash,
        int idleTimeoutMinutes,
        byte platform,
        string? ipAddress,
        string? deviceInfo,
        int createdBy,
        CancellationToken cancellationToken = default);

    /// <summary><c>dbo.SvaUsrSess</c> (N6): per-request validation; revokes on idle / absolute expiry; throttled touch.</summary>
    Task<SessionValidationStatus> ValidateAsync(
        int sessionId,
        int userId,
        int idleTimeoutMinutes,
        int touchIntervalSeconds,
        byte platform,
        int modifiedBy,
        CancellationToken cancellationToken = default);

    /// <summary><c>dbo.SupUserSessRev</c>: revokes one session (<paramref name="sessionId"/>) or all sessions of the user (null). Rows affected.</summary>
    Task<int> RevokeAsync(
        int userId,
        int? sessionId,
        string revokedReason,
        int modifiedBy,
        CancellationToken cancellationToken = default);
}

/// <summary>Mirrors the <c>SessionStatus</c> column of <c>dbo.SvaUsrSess</c> (N6).</summary>
public enum SessionValidationStatus
{
    Valid = 0,
    NotFoundOrUserMismatch = 1,
    Revoked = 2,
    Expired = 3,
    Idle = 4,
    UserInactive = 5,
    UserLocked = 6,
    PlatformDenied = 7
}

/// <summary>Output of N1: the new session Id (the <c>sid</c> claim) and its absolute end (UTC).</summary>
public sealed record CreatedSessionReadModel(int SessionId, DateTime ExpiresUtc);

/// <summary>The <c>SreUsrSessByTokenHash</c> row. Dates are UTC (database clock).</summary>
public sealed class SessionReadModel
{
    public int SessionId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastActDate { get; set; }
    public DateTime ExpiresDate { get; set; }
    public DateTime? RevokedDate { get; set; }
    public string? RevokedReason { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public bool PwdChangeReq { get; set; }
    public bool? AccountIsActive { get; set; }
    public bool? CanAccessWeb { get; set; }
    public bool? CanAccessMobile { get; set; }
    public bool IsExpired { get; set; }
    public bool IsIdle { get; set; }
}
