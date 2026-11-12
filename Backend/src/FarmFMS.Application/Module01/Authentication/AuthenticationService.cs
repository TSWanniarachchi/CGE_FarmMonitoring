using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Common.Exceptions;
using FarmFMS.Application.Common.Extensions;
using FarmFMS.Application.Common.Identity;
using FarmFMS.Application.Common.Results;
using FarmFMS.Domain.Module01;
using FarmFMS.Domain.Module01.Authentication;
using Microsoft.Extensions.Logging;

/*
 * Description  : Login, refresh and logout use cases (Step 0 spec sections 3.2 - 3.4, 6.3, 8.6).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Authentication flows exactly as specified in Step00_Authentication_Foundation_Spec sections 3.2 - 3.4.
/// <para>
/// <b>Audit actor (AD-14 explicit exception, spec section 6.3, D-18).</b> These writes happen before a request user
/// exists, so <see cref="IAuditActorService"/> cannot supply the actor from the request:
/// successful login, refresh and logout are recorded with the <i>verified user's own Id</i> (password or refresh token
/// proven); failed-login history (known or unknown user) and reuse / expiry / account-state revocations are recorded with
/// the System user inside <c>BeginSystemOperation("Login")</c> / <c>("SessionExpiry")</c>. When the System user cannot be
/// resolved the operation fails closed with a generic Failure and nothing is written: an actor is never invented.
/// </para>
/// <para>
/// <b>Fail closed (M-2).</b> The System actor is resolved once, before any credential is evaluated; without it login and
/// refresh return the generic Failure for every outcome (even a correct password), so a missing System user can never
/// turn into a password oracle without lockout.
/// </para>
/// <para>
/// <b>Accepted risk (L-7).</b> Failure counting is atomic per attempt, but concurrent wrong attempts that are already past
/// the locked check can each be counted before the 5th locks the account, so a burst of parallel guesses may exceed 5.
/// </para>
/// <para>Logs never contain passwords, tokens, hashes, payload bytes or unmasked usernames.</para>
/// </summary>
internal sealed class AuthenticationService : IAuthenticationService
{
    private const string LoginOperation = "Login";
    private const string SessionExpiryOperation = "SessionExpiry";

    // Business errors raised by the procedures are matched on the catalogue error code, never on SQL numbers.

    /// <summary>Catalogue code of <c>THROW 50001</c> of <c>dbo.ScrUsrSessRotate</c> (Auth.SessionInvalid).</summary>
    private const string SessionInvalidErrorCode = "Auth.SessionInvalid";

    /// <summary>Catalogue code of <c>THROW 50002</c> of <c>dbo.ScrUsrSess</c> (Auth.UserNotActive).</summary>
    private const string UserNotActiveErrorCode = "Auth.UserNotActive";

    private readonly ILoginRepository _login;
    private readonly IUserSessionRepository _sessions;
    private readonly ILoginHistoryRepository _history;
    private readonly IPasswordProtector _passwordProtector;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly ISessionTokenFactory _tokenFactory;
    private readonly ISessionSettings _settings;
    private readonly IAuditActorService _auditActor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        ILoginRepository login,
        IUserSessionRepository sessions,
        ILoginHistoryRepository history,
        IPasswordProtector passwordProtector,
        IAccessTokenIssuer accessTokenIssuer,
        ISessionTokenFactory tokenFactory,
        ISessionSettings settings,
        IAuditActorService auditActor,
        IUnitOfWork unitOfWork,
        ILogger<AuthenticationService> logger)
    {
        _login = login ?? throw new ArgumentNullException(nameof(login));
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _passwordProtector = passwordProtector ?? throw new ArgumentNullException(nameof(passwordProtector));
        _accessTokenIssuer = accessTokenIssuer ?? throw new ArgumentNullException(nameof(accessTokenIssuer));
        _tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _auditActor = auditActor ?? throw new ArgumentNullException(nameof(auditActor));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Login (spec 3.2)

    public async Task<Result<TokenResponse>> LoginAsync(
        LoginRequest request,
        ClientContext client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        client ??= ClientContext.Empty;

        // 1. Normalize the username (D-21). Blank / oversized input cannot match any account.
        var username = UsernameNormalizer.Normalize(request.Username);
        var password = request.Password;
        if (username.Length is 0 or > Module01FieldLengths.Username
            || string.IsNullOrEmpty(password)
            || password.Length > PasswordPolicy.MaxLength)
        {
            _logger.LogWarning("LoginFailed: reason {Reason}", "InvalidInput");
            return AuthErrors.InvalidCredentials;
        }

        // M-2: resolve the System actor once, before evaluating anything (fail closed for every outcome).
        var systemActor = await GetSystemActorAsync(LoginOperation, cancellationToken);
        if (systemActor.IsFailure)
            return systemActor.Error;

        var actorId = systemActor.Value;

        // 2. Read-only credential retrieval (D-03).
        var user = await _login.GetCredentialsAsync(username, cancellationToken);

        // 3. Unknown user.
        if (user is null)
            return await RejectLoginAsync(null, username, LoginFailureReasons.UnknownUser, AuthErrors.InvalidCredentials, client, actorId, cancellationToken);

        // 4. Inactive: never decrypt (design 6.2.3).
        if (!user.IsActive)
            return await RejectLoginAsync(user.UserId, user.Username, LoginFailureReasons.Inactive, AuthErrors.InvalidCredentials, client, actorId, cancellationToken);

        // 5. Locked: generic 401 (D-12); no decryption, not counted.
        if (user.IsLocked)
            return await RejectLoginAsync(user.UserId, user.Username, LoginFailureReasons.Locked, AuthErrors.InvalidCredentials, client, actorId, cancellationToken);

        // 6. Verify the password.
        var payload = new EncryptedPassword(user.EncPassword, user.PasswordNonce, user.PasswordTag, user.KeyVersion);
        var verification = _passwordProtector.Verify(password, payload, user.UserId);

        switch (verification)
        {
            case PasswordVerificationResult.IntegrityFailure:
            case PasswordVerificationResult.UnknownKeyVersion:
                // P-23: not counted; security event at Error (alert hook). UserId + KeyVersion only.
                _logger.LogError(
                    "CredentialIntegrityFailure: user {UserId}, key version {KeyVersion}, outcome {Outcome}",
                    user.UserId, user.KeyVersion, verification);
                return await RejectLoginAsync(user.UserId, user.Username, LoginFailureReasons.CredentialError, AuthErrors.InvalidCredentials, client, actorId, cancellationToken);

            case PasswordVerificationResult.Mismatch:
                return await RejectWrongPasswordAsync(user, client, actorId, cancellationToken);

            case PasswordVerificationResult.Match:
                break;

            default:
                throw new InvalidOperationException("Unexpected password verification result.");
        }

        // 7. Platform access (D-13): no account, inactive account or CanAccessWeb = 0. Not counted.
        if (user.UserAccountId is null || user.AccountIsActive != true || user.CanAccessWeb != true)
            return await RejectLoginAsync(user.UserId, user.Username, LoginFailureReasons.PlatformDenied, AuthErrors.PlatformAccessDenied, client, actorId, cancellationToken);

        // 8. Success: one unit of work. Actor = the verified user's own Id (spec 6.3).
        var refreshToken = _tokenFactory.Create();
        CreatedSessionReadModel? session;
        try
        {
            var outcome = await _unitOfWork.ExecuteInTransactionAsync<Result<CreatedSessionReadModel>>(
                async token =>
                {
                    var rows = await _login.RecordLoginSuccessAsync(user.UserId, token);
                    if (rows == 0)
                        return AuthErrors.InvalidCredentials; // user no longer active / unlocked: roll back

                    var created = await _sessions.CreateAsync(
                        user.UserId,
                        refreshToken.Hash,
                        _settings.AbsoluteLifetimeMinutes,
                        client.IPAddress,
                        client.DeviceInfo,
                        user.UserId,
                        token);

                    await _history.CreateAsync(
                        user.UserId, user.Username, LoginEventType.Success,
                        client.IPAddress, client.DeviceInfo, null, user.UserId, token);

                    return created;
                },
                cancellationToken: cancellationToken);

            if (outcome.IsFailure)
            {
                _logger.LogWarning("LoginFailed: reason {Reason}, user {UserId}", "StateChanged", user.UserId);
                return outcome.Error;
            }

            session = outcome.Value;
        }
        catch (DataAccessException ex) when (ex.Error.Code == UserNotActiveErrorCode)
        {
            _logger.LogWarning("LoginFailed: reason {Reason}, user {UserId}", "StateChanged", user.UserId);
            return AuthErrors.InvalidCredentials;
        }

        // 9. Issue the access token.
        var access = _accessTokenIssuer.Issue(user.UserId, session.SessionId, user.Username);
        _logger.LogInformation("LoginSucceeded: user {UserId}, session {SessionId}", user.UserId, session.SessionId);

        return BuildResponse(
            access, refreshToken, AsUtc(session.ExpiresUtc),
            user.UserId, user.Username, user.FirstName, user.LastName, user.PwdChangeReq);
    }

    /// <summary>Failed-login history (System actor); no counting. Returns the given error.</summary>
    private async Task<Result<TokenResponse>> RejectLoginAsync(
        int? userId,
        string username,
        string reason,
        Error error,
        ClientContext client,
        int actorId,
        CancellationToken cancellationToken)
    {
        LogLoginFailed(userId, username, reason);

        await _history.CreateAsync(
            userId, username, LoginEventType.Failed,
            client.IPAddress, client.DeviceInfo, reason, actorId, cancellationToken);

        return error;
    }

    /// <summary>Wrong password: count (N3) and history in one unit of work (System actor for the history).</summary>
    private async Task<Result<TokenResponse>> RejectWrongPasswordAsync(
        LoginCredentialReadModel user,
        ClientContext client,
        int actorId,
        CancellationToken cancellationToken)
    {
        LogLoginFailed(user.UserId, user.Username, LoginFailureReasons.InvalidPassword);

        var outcome = new LoginFailureOutcome(0, false);

        await _unitOfWork.ExecuteInTransactionAsync<Result>(
            async token =>
            {
                outcome = await _login.RecordLoginFailureAsync(user.UserId, SessionPolicy.MaxFailedLoginAttempts, token);

                await _history.CreateAsync(
                    user.UserId, user.Username, LoginEventType.Failed,
                    client.IPAddress, client.DeviceInfo, LoginFailureReasons.InvalidPassword, actorId, token);

                return Result.Success();
            },
            cancellationToken: cancellationToken);

        if (outcome.IsLocked)
            _logger.LogWarning("AccountLocked: user {UserId}", user.UserId);

        return AuthErrors.InvalidCredentials;
    }

    private void LogLoginFailed(int? userId, string username, string reason)
    {
        if (userId is not null)
            _logger.LogWarning("LoginFailed: reason {Reason}, user {UserId}", reason, userId);
        else
            _logger.LogWarning("LoginFailed: reason {Reason}, username {MaskedUsername}", reason, username.Mask());
    }

    #endregion

    #region Refresh (spec 3.3)

    public async Task<Result<TokenResponse>> RefreshAsync(
        RefreshRequest request,
        ClientContext client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        client ??= ClientContext.Empty;

        // 1. Decode and hash.
        if (!_tokenFactory.TryHash(request.RefreshToken, out var hash))
        {
            _logger.LogInformation("RefreshRejected: reason {Reason}", "Malformed");
            return AuthErrors.InvalidRefreshToken;
        }

        // M-2: resolve the System actor once, before evaluating the session, so that reuse / expiry handling can
        // never be skipped while a rotation would still succeed (fail closed).
        var systemActor = await GetSystemActorAsync(SessionExpiryOperation, cancellationToken);
        if (systemActor.IsFailure)
            return systemActor.Error;

        var actorId = systemActor.Value;

        // 2. Unknown token.
        var session = await _sessions.GetByTokenHashAsync(hash, SessionPolicy.IdleTimeoutMinutes, cancellationToken);
        if (session is null)
        {
            _logger.LogWarning("RefreshRejected: reason {Reason}", "UnknownToken");
            return AuthErrors.InvalidRefreshToken;
        }

        // 3-6. Reuse, revoked, expired, idle, account state.
        var rejection = await RejectSessionAsync(session, actorId, cancellationToken);
        if (rejection is not null)
            return rejection;

        // 7. Rotate. Actor = the verified user's own Id (spec 6.3).
        var next = _tokenFactory.Create();
        int newSessionId;
        try
        {
            newSessionId = await _sessions.RotateAsync(
                session.SessionId,
                next.Hash,
                SessionPolicy.IdleTimeoutMinutes,
                SessionPolicy.PlatformWeb,
                client.IPAddress,
                client.DeviceInfo,
                session.UserId,
                cancellationToken);
        }
        catch (DataAccessException ex) when (ex.Error.Code == SessionInvalidErrorCode)
        {
            // L-2: N5 refused. Find out why before deciding: only a concurrent rotation is reuse.
            return await HandleRotationRefusedAsync(session, hash, actorId, cancellationToken);
        }

        // 8. New access token with the new session id. No LoginHistory event for refresh (spec event list).
        var access = _accessTokenIssuer.Issue(session.UserId, newSessionId, session.Username);
        _logger.LogInformation(
            "TokenRefreshed: user {UserId}, session {SessionId}, previous session {PreviousSessionId}",
            session.UserId, newSessionId, session.SessionId);

        return BuildResponse(
            access, next, AsUtc(session.ExpiresDate),
            session.UserId, session.Username, session.FirstName, session.LastName, session.PwdChangeReq);
    }

    /// <summary>
    /// The rejection for a session that must not be rotated (spec 3.3 steps 3-6), or <c>null</c> when it is usable.
    /// </summary>
    private async Task<Result<TokenResponse>?> RejectSessionAsync(
        SessionReadModel session,
        int actorId,
        CancellationToken cancellationToken)
    {
        // A rotated token presented again is reuse: revoke every session of the user.
        if (session.RevokedDate is not null &&
            string.Equals(session.RevokedReason, SessionRevokedReasons.Rotated, StringComparison.Ordinal))
        {
            return await HandleReuseAsync(session.UserId, session.SessionId, actorId, cancellationToken);
        }

        // Revoked for another reason.
        if (session.RevokedDate is not null)
        {
            _logger.LogInformation(
                "RefreshRejected: reason {Reason}, user {UserId}, session {SessionId}", "Revoked", session.UserId, session.SessionId);
            return AuthErrors.InvalidRefreshToken;
        }

        // Absolute or idle expiry, then account state: revoke this session only.
        if (session.IsExpired)
            return await RevokeAndRejectAsync(session, SessionRevokedReasons.Expired, actorId, cancellationToken);
        if (session.IsIdle)
            return await RevokeAndRejectAsync(session, SessionRevokedReasons.IdleTimeout, actorId, cancellationToken);
        if (!session.IsActive || session.IsLocked || session.AccountIsActive != true || session.CanAccessWeb != true)
            return await RevokeAndRejectAsync(session, SessionRevokedReasons.AccountState, actorId, cancellationToken);

        return null;
    }

    /// <summary>
    /// N5 raised 50001 (the session was no longer active). Re-read it: a concurrent rotation is reuse (revoke all);
    /// idle / expiry / account state revoke that one session with the matching reason; anything else is a plain 401
    /// (no false <c>RefreshTokenReuse</c>).
    /// </summary>
    private async Task<Result<TokenResponse>> HandleRotationRefusedAsync(
        SessionReadModel previous,
        byte[] tokenHash,
        int actorId,
        CancellationToken cancellationToken)
    {
        var current = await _sessions.GetByTokenHashAsync(tokenHash, SessionPolicy.IdleTimeoutMinutes, cancellationToken);
        if (current is null)
        {
            _logger.LogWarning(
                "RefreshRejected: reason {Reason}, user {UserId}, session {SessionId}", "SessionGone", previous.UserId, previous.SessionId);
            return AuthErrors.InvalidRefreshToken;
        }

        var rejection = await RejectSessionAsync(current, actorId, cancellationToken);
        if (rejection is not null)
            return rejection;

        _logger.LogWarning(
            "RefreshRejected: reason {Reason}, user {UserId}, session {SessionId}", "RotationRefused", current.UserId, current.SessionId);
        return AuthErrors.InvalidRefreshToken;
    }

    private async Task<Result<TokenResponse>> HandleReuseAsync(int userId, int sessionId, int actorId, CancellationToken cancellationToken)
    {
        var revoked = await _sessions.RevokeAsync(
            userId, null, SessionRevokedReasons.RefreshTokenReuse, actorId, cancellationToken);

        _logger.LogWarning(
            "RefreshTokenReuse: user {UserId}, session {SessionId}, revoked sessions {RevokedCount}",
            userId, sessionId, revoked);

        return AuthErrors.InvalidRefreshToken;
    }

    private async Task<Result<TokenResponse>> RevokeAndRejectAsync(
        SessionReadModel session,
        string revokedReason,
        int actorId,
        CancellationToken cancellationToken)
    {
        await _sessions.RevokeAsync(session.UserId, session.SessionId, revokedReason, actorId, cancellationToken);

        _logger.LogInformation(
            "RefreshRejected: reason {Reason}, user {UserId}, session {SessionId}",
            revokedReason, session.UserId, session.SessionId);

        return AuthErrors.InvalidRefreshToken;
    }

    #endregion

    #region Logout (spec 3.4)

    public async Task<Result> LogoutAsync(
        LogoutRequest request,
        ClientContext client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        client ??= ClientContext.Empty;

        // 1. Invalid, unknown or already revoked: success without writes (no oracle).
        if (!_tokenFactory.TryHash(request.RefreshToken, out var hash))
            return Result.Success();

        var session = await _sessions.GetByTokenHashAsync(hash, SessionPolicy.IdleTimeoutMinutes, cancellationToken);
        if (session is null || session.RevokedDate is not null)
            return Result.Success();

        // 2. Revoke + history in one unit of work. Actor = the verified user's own Id (spec 6.3).
        await _unitOfWork.ExecuteInTransactionAsync<Result>(
            async token =>
            {
                await _sessions.RevokeAsync(
                    session.UserId, session.SessionId, SessionRevokedReasons.Logout, session.UserId, token);

                await _history.CreateAsync(
                    session.UserId, session.Username, LoginEventType.Logout,
                    client.IPAddress, client.DeviceInfo, null, session.UserId, token);

                return Result.Success();
            },
            cancellationToken: cancellationToken);

        _logger.LogInformation("LogoutSucceeded: user {UserId}, session {SessionId}", session.UserId, session.SessionId);
        return Result.Success();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// The System user as audit actor for the unauthenticated writes (spec 6.3). Any failure becomes the generic
    /// <see cref="AuditErrors.SystemUserNotConfigured"/> (500), never an invented actor.
    /// </summary>
    private async Task<Result<int>> GetSystemActorAsync(string operationName, CancellationToken cancellationToken)
    {
        using (_auditActor.BeginSystemOperation(operationName))
        {
            var actor = await _auditActor.GetActorIdAsync(cancellationToken);
            if (actor.IsSuccess)
                return actor;
        }

        _logger.LogError(
            "The System user could not be resolved for operation {OperationName}; nothing was written (fail closed)",
            operationName);

        return AuditErrors.SystemUserNotConfigured;
    }

    private TokenResponse BuildResponse(
        AccessToken access,
        SessionToken refreshToken,
        DateTime sessionExpiresUtc,
        int userId,
        string username,
        string firstName,
        string? lastName,
        bool passwordChangeRequired) => new()
    {
        AccessToken = access.Token,
        AccessTokenExpiresUtc = AsUtc(access.ExpiresUtc),
        RefreshToken = refreshToken.RawToken,
        SessionExpiresUtc = sessionExpiresUtc,
        IdleTimeoutMinutes = SessionPolicy.IdleTimeoutMinutes,
        PasswordChangeRequired = passwordChangeRequired,
        User = new AuthenticatedUserDto
        {
            Id = userId,
            Username = username,
            FirstName = firstName,
            LastName = lastName
        }
    };

    /// <summary>Database DATETIME2 values come back with Kind Unspecified; they are UTC (SYSUTCDATETIME).</summary>
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    #endregion
}
