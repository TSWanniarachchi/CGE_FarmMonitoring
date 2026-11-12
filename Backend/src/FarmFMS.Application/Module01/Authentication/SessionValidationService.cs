using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Common.Identity;
using FarmFMS.Application.Common.Results;
using Microsoft.Extensions.Logging;

/*
 * Description  : Per-request session validation (N6) (Step 0 spec sections 3.5, 8.6, D-08).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Validates the session behind an access token on every protected request: revocation, absolute expiry, the 30-minute
/// idle timeout, user active / unlocked and platform access. <c>dbo.SvaUsrSess</c> may revoke an idle or expired session
/// and records <c>ModifiedBy</c>: the System user inside <c>BeginSystemOperation("SessionExpiry")</c> (AD-14 explicit
/// exception, spec 6.3). When the System user cannot be resolved the request fails closed with a generic Failure.
/// </summary>
internal sealed class SessionValidationService : ISessionValidationService
{
    private const string SessionExpiryOperation = "SessionExpiry";

    private readonly IUserSessionRepository _sessions;
    private readonly ISessionSettings _settings;
    private readonly IAuditActorService _auditActor;
    private readonly ILogger<SessionValidationService> _logger;

    public SessionValidationService(
        IUserSessionRepository sessions,
        ISessionSettings settings,
        IAuditActorService auditActor,
        ILogger<SessionValidationService> logger)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _auditActor = auditActor ?? throw new ArgumentNullException(nameof(auditActor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> ValidateAsync(int sessionId, int userId, CancellationToken cancellationToken = default)
    {
        if (sessionId <= 0 || userId <= 0)
            return AuthErrors.SessionInvalid;

        Result<int> actor;
        using (_auditActor.BeginSystemOperation(SessionExpiryOperation))
        {
            actor = await _auditActor.GetActorIdAsync(cancellationToken);
        }

        if (actor.IsFailure)
        {
            _logger.LogError(
                "The System user could not be resolved for operation {OperationName}; the request is rejected (fail closed)",
                SessionExpiryOperation);
            return AuditErrors.SystemUserNotConfigured;
        }

        var status = await _sessions.ValidateAsync(
            sessionId,
            userId,
            SessionPolicy.IdleTimeoutMinutes,
            _settings.ActivityTouchIntervalSeconds,
            SessionPolicy.PlatformWeb,
            actor.Value,
            cancellationToken);

        if (status == SessionValidationStatus.Valid)
            return Result.Success();

        var level = status is SessionValidationStatus.Idle or SessionValidationStatus.Expired
            ? LogLevel.Information
            : LogLevel.Warning;
        _logger.Log(level, "SessionRejected: user {UserId}, session {SessionId}, status {Status}", userId, sessionId, status);

        return AuthErrors.SessionInvalid;
    }
}
