using System.Security.Cryptography;
using FarmFMS.Application.Common.Identity;
using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Domain.Module01.Authentication;
using FarmFMS.Infrastructure.Security;
using FarmFMS.UnitTests.Module01.Fakes;
using Microsoft.Extensions.Logging;

namespace FarmFMS.UnitTests.Module01;

/// <summary>
/// Login / refresh / logout per Step 0 spec 3.2 - 3.4 and 10.1 (fake repositories, fake protector, real token factory).
/// </summary>
public class AuthenticationServiceTests
{
    private const int UserId = 42;
    private const int SystemId = 7;
    private const string Password = "Sup3r-Secret!";

    private sealed class Harness
    {
        public CallLog Log { get; } = new();
        public FakeLoginRepository Login { get; }
        public FakeUserSessionRepository Sessions { get; }
        public FakeLoginHistoryRepository History { get; }
        public FakePasswordProtector Protector { get; } = new();
        public FakeAccessTokenIssuer Issuer { get; } = new();
        public SessionTokenFactory Tokens { get; } = new();
        public FakeSessionSettings Settings { get; } = new();
        public FakeAuditActor Actor { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; }
        public ListLogger<AuthenticationService> Logger { get; } = new();
        public IAuthenticationService Service { get; }

        public Harness()
        {
            Login = new FakeLoginRepository(Log) { Credentials = ValidUser() };
            Sessions = new FakeUserSessionRepository(Log);
            History = new FakeLoginHistoryRepository(Log);
            UnitOfWork = new FakeUnitOfWork(Log);

            Service = CreateService();
        }

        private IAuthenticationService CreateService() => new AuthenticationService(
            Login, Sessions, History, Protector, Issuer, Tokens, Settings, Actor, UnitOfWork, Logger);
    }

    private static LoginCredentialReadModel ValidUser() => new()
    {
        UserId = UserId,
        Username = "jsmith",
        FirstName = "Jane",
        LastName = "Smith",
        EncPassword = [1, 2, 3],
        PasswordNonce = new byte[12],
        PasswordTag = new byte[16],
        KeyVersion = "test-1",
        IsActive = true,
        IsLocked = false,
        FailedLogCount = 0,
        PwdChangeReq = false,
        UserAccountId = 5,
        AccountIsActive = true,
        CanAccessWeb = true,
        CanAccessMobile = false
    };

    private static LoginRequest Request(string username = "jsmith", string password = Password) =>
        new() { Username = username, Password = password };

    private static readonly ClientContext Client = new("10.1.2.3", "TestAgent/1.0");

    #region Login: rejections

    [Fact]
    public async Task Unknown_user_gets_the_generic_401_and_a_system_actor_history_row()
    {
        var h = new Harness();
        h.Login.Credentials = null;

        var result = await h.Service.LoginAsync(Request("ghost"), Client);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        Assert.Equal(0, h.Protector.VerifyCalls);
        Assert.DoesNotContain("N3", h.Log);
        var entry = Assert.Single(h.History.Entries);
        Assert.Null(entry.UserId);
        Assert.Equal("ghost", entry.Username);
        Assert.Equal(LoginEventType.Failed, entry.EventType);
        Assert.Equal(LoginFailureReasons.UnknownUser, entry.FailureReason);
        Assert.Equal(SystemId, entry.CreatedBy);
        Assert.Equal("10.1.2.3", entry.IpAddress);
        Assert.Equal(["Login"], h.Actor.Operations);
    }

    [Fact]
    public async Task Inactive_user_is_rejected_without_decryption()
    {
        var h = new Harness();
        h.Login.Credentials!.IsActive = false;

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
        Assert.Equal(0, h.Protector.VerifyCalls);
        var entry = Assert.Single(h.History.Entries);
        Assert.Equal(UserId, entry.UserId);
        Assert.Equal(LoginFailureReasons.Inactive, entry.FailureReason);
        Assert.Equal(SystemId, entry.CreatedBy);
    }

    [Fact]
    public async Task Locked_user_is_rejected_without_decryption_and_is_not_counted()
    {
        var h = new Harness();
        h.Login.Credentials!.IsLocked = true;

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error); // generic: same body as every other failure (D-12)
        Assert.Equal(0, h.Protector.VerifyCalls);
        Assert.DoesNotContain("N3", h.Log);
        Assert.Equal(LoginFailureReasons.Locked, Assert.Single(h.History.Entries).FailureReason);
    }

    [Fact]
    public async Task Wrong_password_counts_and_writes_history_in_one_unit_of_work()
    {
        var h = new Harness();
        h.Protector.Result = PasswordVerificationResult.Mismatch;

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
        Assert.True(h.Log.HappenedInOrder("BEGIN", "N3", "History", "COMMIT"));
        Assert.Equal(SessionPolicy.MaxFailedLoginAttempts, h.Login.LastMaxFailedAttempts);
        Assert.Equal(5, h.Login.LastMaxFailedAttempts);
        var entry = Assert.Single(h.History.Entries);
        Assert.Equal(LoginFailureReasons.InvalidPassword, entry.FailureReason);
        Assert.Equal(UserId, entry.UserId);
        Assert.Equal(SystemId, entry.CreatedBy);
        Assert.DoesNotContain(h.Logger.Entries, e => e.Level == LogLevel.Error);
        Assert.False(h.Logger.Contains(LogLevel.Warning, "AccountLocked"));
    }

    [Fact]
    public async Task The_failure_that_locks_the_account_is_logged_as_AccountLocked()
    {
        var h = new Harness();
        h.Protector.Result = PasswordVerificationResult.Mismatch;
        h.Login.FailureOutcome = new LoginFailureOutcome(1, true);

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
        Assert.True(h.Logger.Contains(LogLevel.Warning, "AccountLocked"));
        Assert.True(h.Logger.Contains(LogLevel.Warning, UserId.ToString()));
    }

    [Theory]
    [InlineData(PasswordVerificationResult.IntegrityFailure)]
    [InlineData(PasswordVerificationResult.UnknownKeyVersion)]
    public async Task Credential_errors_are_not_counted_and_raise_an_Error_log_with_user_and_key_version_only(PasswordVerificationResult outcome)
    {
        var h = new Harness();
        h.Protector.Result = outcome;

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
        Assert.DoesNotContain("N3", h.Log);
        Assert.Equal(LoginFailureReasons.CredentialError, Assert.Single(h.History.Entries).FailureReason);

        var error = Assert.Single(h.Logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains(UserId.ToString(), error.Message);
        Assert.Contains("test-1", error.Message);
        Assert.DoesNotContain(Password, error.Message);
        Assert.DoesNotContain("jsmith", error.Message);
    }

    [Theory]
    [InlineData("noAccount")]
    [InlineData("accountInactive")]
    [InlineData("noWebAccess")]
    public async Task Correct_password_without_web_access_is_403_and_not_counted(string variant)
    {
        var h = new Harness();
        var user = h.Login.Credentials!;
        switch (variant)
        {
            case "noAccount":
                user.UserAccountId = null;
                user.AccountIsActive = null;
                user.CanAccessWeb = null;
                break;
            case "accountInactive":
                user.AccountIsActive = false;
                break;
            default:
                user.CanAccessWeb = false;
                break;
        }

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.Same(AuthErrors.PlatformAccessDenied, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
        Assert.DoesNotContain("N3", h.Log);
        Assert.DoesNotContain("N1", h.Log);
        var entry = Assert.Single(h.History.Entries);
        Assert.Equal(LoginFailureReasons.PlatformDenied, entry.FailureReason);
        Assert.Equal(SystemId, entry.CreatedBy);
    }

    [Fact]
    public async Task Without_a_System_user_failed_logins_fail_closed_and_write_nothing()
    {
        var h = new Harness();
        h.Actor.SystemUserId = null;
        h.Login.Credentials = null;

        var result = await h.Service.LoginAsync(Request("ghost"), Client);

        Assert.Same(AuditErrors.SystemUserNotConfigured, result.Error);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        Assert.Empty(h.History.Entries);
        Assert.Equal(0, h.Login.CredentialLookups); // resolved once, before anything is evaluated
        Assert.True(h.Logger.Contains(LogLevel.Error, "System user"));
    }

    [Fact]
    public async Task Without_a_System_user_a_wrong_password_fails_closed_without_credential_evaluation()
    {
        var h = new Harness();
        h.Actor.SystemUserId = null;
        h.Protector.Result = PasswordVerificationResult.Mismatch;

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.Same(AuditErrors.SystemUserNotConfigured, result.Error);
        Assert.Equal(0, h.Login.CredentialLookups);
        Assert.Equal(0, h.Protector.VerifyCalls);
        Assert.DoesNotContain("N3", h.Log);
        Assert.Empty(h.History.Entries);
        Assert.DoesNotContain("BEGIN", h.Log);
    }

    [Fact]
    public async Task Without_a_System_user_the_correct_password_also_fails_closed_no_password_oracle()
    {
        var h = new Harness();
        h.Actor.SystemUserId = null;
        h.Protector.Result = PasswordVerificationResult.Match;

        var result = await h.Service.LoginAsync(Request(), Client);

        // Same generic Failure as a wrong password: success must not be distinguishable from an uncounted failure.
        Assert.Same(AuditErrors.SystemUserNotConfigured, result.Error);
        Assert.Equal(ErrorType.Failure, result.Error.Type);
        Assert.Equal(0, h.Protector.VerifyCalls);
        Assert.DoesNotContain("N1", h.Log);
        Assert.DoesNotContain("N2", h.Log);
        Assert.Empty(h.Sessions.Creates);
        Assert.Empty(h.Issuer.Issued);
    }

    #endregion

    #region Login: success

    [Fact]
    public async Task Successful_login_commits_N2_N1_and_history_with_the_users_own_id_as_actor()
    {
        var h = new Harness();

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.True(result.IsSuccess);
        Assert.True(h.Log.HappenedInOrder("GetCredentials", "BEGIN", "N2", "N1", "History", "COMMIT"));

        var session = Assert.Single(h.Sessions.Creates);
        Assert.Equal(UserId, session.UserId);
        Assert.Equal(UserId, session.CreatedBy);
        Assert.Equal(720, session.AbsoluteLifetimeMinutes);
        Assert.Equal(32, session.TokenHash.Length);
        Assert.Equal("10.1.2.3", session.IpAddress);
        Assert.Equal("TestAgent/1.0", session.DeviceInfo);

        var entry = Assert.Single(h.History.Entries);
        Assert.Equal(LoginEventType.Success, entry.EventType);
        Assert.Equal(UserId, entry.CreatedBy);
        Assert.Null(entry.FailureReason);

        // The user's own id is the actor: no System operation is started for a successful login.
        Assert.Equal(["Login"], h.Actor.Operations); // resolved once up front (fail closed); every success write uses the user's own Id
        Assert.Equal((UserId, 501, "jsmith"), Assert.Single(h.Issuer.Issued));
    }

    [Fact]
    public async Task Successful_login_returns_the_token_response_contract()
    {
        var h = new Harness();
        h.Login.Credentials!.PwdChangeReq = true;

        var response = (await h.Service.LoginAsync(Request(), Client)).Value;

        Assert.Equal("access-token-501", response.AccessToken);
        Assert.Equal(DateTimeKind.Utc, response.AccessTokenExpiresUtc.Kind);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(30, response.IdleTimeoutMinutes);
        Assert.True(response.PasswordChangeRequired);
        Assert.Equal(DateTimeKind.Utc, response.SessionExpiresUtc.Kind);
        Assert.Equal(new DateTime(2026, 11, 13, 6, 0, 0), response.SessionExpiresUtc);
        Assert.Equal(UserId, response.User.Id);
        Assert.Equal("jsmith", response.User.Username);
        Assert.Equal("Jane", response.User.FirstName);
        Assert.Equal("Smith", response.User.LastName);

        // The refresh token is the 43-character token whose SHA-256 hash was stored.
        Assert.Equal(43, response.RefreshToken.Length);
        Assert.True(h.Tokens.TryHash(response.RefreshToken, out var hash));
        Assert.Equal(hash, h.Sessions.Creates[0].TokenHash);
        Assert.True(h.Logger.Contains(LogLevel.Information, "LoginSucceeded"));
    }

    [Fact]
    public async Task When_N2_updates_no_row_the_login_rolls_back_and_returns_401()
    {
        var h = new Harness();
        h.Login.SuccessRows = 0;

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
        Assert.True(h.Log.HappenedInOrder("BEGIN", "N2", "ROLLBACK"));
        Assert.DoesNotContain("N1", h.Log);
        Assert.Empty(h.History.Entries);
        Assert.Empty(h.Issuer.Issued);
    }

    [Fact]
    public async Task When_N1_reports_the_user_is_not_active_the_login_rolls_back_and_returns_401()
    {
        var h = new Harness();
        h.Sessions.CreateException = Errors.Database(50002, Error.Unauthorized("Auth.UserNotActive", "Invalid username or password."));

        var result = await h.Service.LoginAsync(Request(), Client);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
        Assert.Contains("ROLLBACK", h.Log);
        Assert.DoesNotContain("History", h.Log);
    }

    [Fact]
    public async Task Concurrent_sessions_are_allowed_each_login_creates_its_own_session()
    {
        var h = new Harness();

        var first = await h.Service.LoginAsync(Request(), Client);
        var second = await h.Service.LoginAsync(Request(), Client);

        Assert.True(first.IsSuccess && second.IsSuccess);
        Assert.NotEqual(first.Value.RefreshToken, second.Value.RefreshToken);
        Assert.Equal(2, h.Sessions.Creates.Count);
    }

    #endregion

    #region Login: input handling

    [Fact]
    public async Task Username_is_trimmed_and_normalized_to_NFC_but_not_lower_cased()
    {
        var h = new Harness();

        await h.Service.LoginAsync(Request("  José  "), Client);

        Assert.Equal("José", h.Login.LastUsername); // composed form, case preserved
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task Blank_username_is_invalid_credentials_without_any_lookup(string username)
    {
        var h = new Harness();

        var result = await h.Service.LoginAsync(Request(username), Client);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
        Assert.Equal(0, h.Login.CredentialLookups);
        Assert.Empty(h.History.Entries);
    }

    [Fact]
    public async Task Empty_or_oversized_password_is_invalid_credentials_without_any_lookup()
    {
        var h = new Harness();

        var empty = await h.Service.LoginAsync(Request(password: ""), Client);
        var tooLong = await h.Service.LoginAsync(Request(password: new string('a', 129)), Client);

        Assert.Same(AuthErrors.InvalidCredentials, empty.Error);
        Assert.Same(AuthErrors.InvalidCredentials, tooLong.Error);
        Assert.Equal(0, h.Login.CredentialLookups);
    }

    [Fact]
    public async Task Logs_never_contain_the_password_or_the_raw_username_of_an_unknown_user()
    {
        var h = new Harness();
        h.Login.Credentials = null;

        await h.Service.LoginAsync(Request("ghost.user.name", "Zq9!very-secret"), Client);

        Assert.DoesNotContain("Zq9!very-secret", h.Logger.All);
        Assert.DoesNotContain("ghost.user.name", h.Logger.All);
        Assert.Contains("***********name", h.Logger.All); // StringExtensions.Mask
    }

    #endregion

    #region Refresh

    private static SessionReadModel ActiveSession() => new()
    {
        SessionId = 400,
        UserId = UserId,
        CreatedDate = new DateTime(2026, 11, 12, 1, 0, 0),
        LastActDate = new DateTime(2026, 11, 12, 5, 55, 0),
        ExpiresDate = new DateTime(2026, 11, 12, 13, 0, 0),
        Username = "jsmith",
        FirstName = "Jane",
        LastName = "Smith",
        IsActive = true,
        IsLocked = false,
        AccountIsActive = true,
        CanAccessWeb = true,
        CanAccessMobile = false,
        PwdChangeReq = false
    };

    private static RefreshRequest RefreshFor(SessionToken token) => new() { RefreshToken = token.RawToken };

    [Fact]
    public async Task Valid_refresh_rotates_the_session_and_returns_new_tokens()
    {
        var h = new Harness();
        var presented = h.Tokens.Create();
        h.Sessions.Session = ActiveSession();

        var result = await h.Service.RefreshAsync(RefreshFor(presented), Client);

        Assert.True(result.IsSuccess);
        Assert.Equal(presented.Hash, h.Sessions.LookedUpHash);

        var rotation = Assert.Single(h.Sessions.Rotations);
        Assert.Equal(400, rotation.CurrentSessionId);
        Assert.Equal(30, rotation.IdleTimeoutMinutes);
        Assert.Equal((byte)1, rotation.Platform);
        Assert.Equal(UserId, rotation.CreatedBy); // the verified user's own Id (spec 6.3)
        Assert.NotEqual(presented.Hash, rotation.NewTokenHash);

        var response = result.Value;
        Assert.NotEqual(presented.RawToken, response.RefreshToken);
        Assert.True(h.Tokens.TryHash(response.RefreshToken, out var newHash));
        Assert.Equal(newHash, rotation.NewTokenHash);
        Assert.Equal("access-token-502", response.AccessToken);
        Assert.Equal(DateTimeKind.Utc, response.SessionExpiresUtc.Kind);
        Assert.Equal(new DateTime(2026, 11, 12, 13, 0, 0), response.SessionExpiresUtc); // inherited ExpiresDate

        Assert.Empty(h.Sessions.Revocations);
        Assert.Empty(h.History.Entries); // no LoginHistory event for refresh
        Assert.Equal(["SessionExpiry"], h.Actor.Operations); // resolved once up front (fail closed); the rotation actor is the user
    }

    [Fact]
    public async Task Refresh_without_a_System_user_fails_closed_even_when_the_rotation_would_succeed()
    {
        var h = new Harness();
        h.Actor.SystemUserId = null;
        h.Sessions.Session = ActiveSession();

        var result = await h.Service.RefreshAsync(RefreshFor(h.Tokens.Create()), Client);

        Assert.Same(AuditErrors.SystemUserNotConfigured, result.Error);
        Assert.DoesNotContain("N4", h.Log);
        Assert.Empty(h.Sessions.Rotations);
        Assert.Empty(h.Issuer.Issued);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-valid-token")]
    public async Task Malformed_refresh_token_is_401_without_a_lookup(string? token)
    {
        var h = new Harness();

        var result = await h.Service.RefreshAsync(new RefreshRequest { RefreshToken = token! }, Client);

        Assert.Same(AuthErrors.InvalidRefreshToken, result.Error);
        Assert.DoesNotContain("N4", h.Log);
    }

    [Fact]
    public async Task Unknown_refresh_token_is_401_and_logged_without_a_user_id()
    {
        var h = new Harness();
        h.Sessions.Session = null;

        var result = await h.Service.RefreshAsync(RefreshFor(h.Tokens.Create()), Client);

        Assert.Same(AuthErrors.InvalidRefreshToken, result.Error);
        var entry = Assert.Single(h.Logger.Entries, e => e.Message.Contains("RefreshRejected"));
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.DoesNotContain("user", entry.Message, StringComparison.OrdinalIgnoreCase); // no user id for an unknown token
        Assert.Empty(h.Sessions.Revocations);
        Assert.Empty(h.History.Entries);
    }

    [Fact]
    public async Task Reusing_a_rotated_token_revokes_all_sessions_of_the_user()
    {
        var h = new Harness();
        var session = ActiveSession();
        session.RevokedDate = new DateTime(2026, 11, 12, 5, 56, 0);
        session.RevokedReason = SessionRevokedReasons.Rotated;
        h.Sessions.Session = session;

        var result = await h.Service.RefreshAsync(RefreshFor(h.Tokens.Create()), Client);

        Assert.Same(AuthErrors.InvalidRefreshToken, result.Error);
        var revoke = Assert.Single(h.Sessions.Revocations);
        Assert.Equal(new RevokeCall(UserId, null, SessionRevokedReasons.RefreshTokenReuse, SystemId), revoke);
        Assert.Empty(h.Sessions.Rotations);
        Assert.Equal(["SessionExpiry"], h.Actor.Operations);
        Assert.True(h.Logger.Contains(LogLevel.Warning, "RefreshTokenReuse"));
        Assert.True(h.Logger.Contains(LogLevel.Warning, "revoked sessions 3"));
    }

    private static readonly Error SessionInvalidError =
        Error.Unauthorized("Auth.SessionInvalid", "Your session is no longer valid. Please sign in again.");

    private static async Task<(Harness Harness, Result<TokenResponse> Result)> RefreshWhereRotationIsRefused(SessionReadModel? afterwards)
    {
        var h = new Harness();
        h.Sessions.Session = ActiveSession();
        h.Sessions.UseSessionOnLaterReads = true;
        h.Sessions.SessionOnLaterReads = afterwards;
        // Matched on the catalogue code (L-1): the SQL number is deliberately different.
        h.Sessions.RotateException = Errors.Database(1, SessionInvalidError);

        var result = await h.Service.RefreshAsync(RefreshFor(h.Tokens.Create()), Client);
        return (h, result);
    }

    [Fact]
    public async Task Concurrent_rotation_is_reuse_only_when_the_row_is_now_Rotated()
    {
        var rotated = ActiveSession();
        rotated.RevokedDate = DateTime.UtcNow;
        rotated.RevokedReason = SessionRevokedReasons.Rotated;

        var (h, result) = await RefreshWhereRotationIsRefused(rotated);

        Assert.Same(AuthErrors.InvalidRefreshToken, result.Error);
        var revoke = Assert.Single(h.Sessions.Revocations);
        Assert.Equal(new RevokeCall(UserId, null, SessionRevokedReasons.RefreshTokenReuse, SystemId), revoke);
        Assert.True(h.Logger.Contains(LogLevel.Warning, "RefreshTokenReuse"));
        Assert.Equal(2, h.Sessions.Lookups); // re-read after N5 refused
    }

    [Theory]
    [InlineData("idle", SessionRevokedReasons.IdleTimeout)]
    [InlineData("expired", SessionRevokedReasons.Expired)]
    [InlineData("account", SessionRevokedReasons.AccountState)]
    public async Task A_rotation_refused_because_the_session_went_idle_expired_or_changed_state_revokes_only_that_session(string kind, string reason)
    {
        var current = ActiveSession();
        if (kind == "idle") current.IsIdle = true;
        if (kind == "expired") current.IsExpired = true;
        if (kind == "account") current.IsActive = false;

        var (h, result) = await RefreshWhereRotationIsRefused(current);

        Assert.Same(AuthErrors.InvalidRefreshToken, result.Error);
        var revoke = Assert.Single(h.Sessions.Revocations);
        Assert.Equal(new RevokeCall(UserId, 400, reason, SystemId), revoke); // this session only, not all
        Assert.False(h.Logger.Contains(LogLevel.Warning, "RefreshTokenReuse")); // no false reuse alarm
    }

    [Fact]
    public async Task A_rotation_refused_because_of_a_concurrent_logout_is_a_plain_401_without_revocation()
    {
        var loggedOut = ActiveSession();
        loggedOut.RevokedDate = DateTime.UtcNow;
        loggedOut.RevokedReason = SessionRevokedReasons.Logout;

        var (h, result) = await RefreshWhereRotationIsRefused(loggedOut);

        Assert.Same(AuthErrors.InvalidRefreshToken, result.Error);
        Assert.Empty(h.Sessions.Revocations);
        Assert.False(h.Logger.Contains(LogLevel.Warning, "RefreshTokenReuse"));
    }

    [Fact]
    public async Task A_rotation_refused_for_a_vanished_or_unexplained_session_is_a_plain_401()
    {
        var (gone, goneResult) = await RefreshWhereRotationIsRefused(null);
        var (unexplained, unexplainedResult) = await RefreshWhereRotationIsRefused(ActiveSession());

        Assert.Same(AuthErrors.InvalidRefreshToken, goneResult.Error);
        Assert.Same(AuthErrors.InvalidRefreshToken, unexplainedResult.Error);
        Assert.Empty(gone.Sessions.Revocations);
        Assert.Empty(unexplained.Sessions.Revocations);
    }

    [Fact]
    public async Task Reuse_without_a_System_user_fails_closed()
    {
        var h = new Harness();
        h.Actor.SystemUserId = null;
        var session = ActiveSession();
        session.RevokedDate = DateTime.UtcNow;
        session.RevokedReason = SessionRevokedReasons.Rotated;
        h.Sessions.Session = session;

        var result = await h.Service.RefreshAsync(RefreshFor(h.Tokens.Create()), Client);

        Assert.Same(AuditErrors.SystemUserNotConfigured, result.Error);
        Assert.Empty(h.Sessions.Revocations);
    }

    [Fact]
    public async Task Token_revoked_for_another_reason_is_401_without_revoking_anything()
    {
        var h = new Harness();
        var session = ActiveSession();
        session.RevokedDate = DateTime.UtcNow;
        session.RevokedReason = SessionRevokedReasons.Logout;
        h.Sessions.Session = session;

        var result = await h.Service.RefreshAsync(RefreshFor(h.Tokens.Create()), Client);

        Assert.Same(AuthErrors.InvalidRefreshToken, result.Error);
        Assert.Empty(h.Sessions.Revocations);
        Assert.Empty(h.Sessions.Rotations);
    }

    [Theory]
    [InlineData("expired", SessionRevokedReasons.Expired)]
    [InlineData("idle", SessionRevokedReasons.IdleTimeout)]
    public async Task Expired_or_idle_sessions_are_revoked_with_the_system_actor(string kind, string expectedReason)
    {
        var h = new Harness();
        var session = ActiveSession();
        if (kind == "expired") session.IsExpired = true; else session.IsIdle = true;
        h.Sessions.Session = session;

        var result = await h.Service.RefreshAsync(RefreshFor(h.Tokens.Create()), Client);

        Assert.Same(AuthErrors.InvalidRefreshToken, result.Error);
        var revoke = Assert.Single(h.Sessions.Revocations);
        Assert.Equal(new RevokeCall(UserId, 400, expectedReason, SystemId), revoke);
        Assert.Empty(h.Sessions.Rotations);
        Assert.Equal(["SessionExpiry"], h.Actor.Operations);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("locked")]
    [InlineData("noAccount")]
    [InlineData("accountInactive")]
    [InlineData("noWebAccess")]
    public async Task Account_state_problems_revoke_the_session(string variant)
    {
        var h = new Harness();
        var session = ActiveSession();
        switch (variant)
        {
            case "inactive": session.IsActive = false; break;
            case "locked": session.IsLocked = true; break;
            case "noAccount": session.AccountIsActive = null; session.CanAccessWeb = null; break;
            case "accountInactive": session.AccountIsActive = false; break;
            default: session.CanAccessWeb = false; break;
        }

        h.Sessions.Session = session;

        var result = await h.Service.RefreshAsync(RefreshFor(h.Tokens.Create()), Client);

        Assert.Same(AuthErrors.InvalidRefreshToken, result.Error);
        var revoke = Assert.Single(h.Sessions.Revocations);
        Assert.Equal(new RevokeCall(UserId, 400, SessionRevokedReasons.AccountState, SystemId), revoke);
        Assert.Empty(h.Sessions.Rotations);
    }

    [Fact]
    public async Task Logs_of_a_refresh_never_contain_tokens()
    {
        var h = new Harness();
        var presented = h.Tokens.Create();
        h.Sessions.Session = ActiveSession();

        var response = (await h.Service.RefreshAsync(RefreshFor(presented), Client)).Value;

        Assert.NotEmpty(h.Logger.Entries); // the refresh is logged; otherwise the checks below would be vacuous
        Assert.DoesNotContain(presented.RawToken, h.Logger.All);
        Assert.DoesNotContain(response.RefreshToken, h.Logger.All);
        Assert.DoesNotContain(response.AccessToken, h.Logger.All);
        Assert.DoesNotContain(Convert.ToHexString(presented.Hash), h.Logger.All, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_revokes_the_session_and_writes_Logout_history_in_one_unit_of_work()
    {
        var h = new Harness();
        h.Sessions.Session = ActiveSession();

        var result = await h.Service.LogoutAsync(new LogoutRequest { RefreshToken = h.Tokens.Create().RawToken }, Client);

        Assert.True(result.IsSuccess);
        Assert.True(h.Log.HappenedInOrder("N4", "BEGIN", "Revoke", "History", "COMMIT"));
        Assert.Equal(new RevokeCall(UserId, 400, SessionRevokedReasons.Logout, UserId), Assert.Single(h.Sessions.Revocations));

        var entry = Assert.Single(h.History.Entries);
        Assert.Equal(LoginEventType.Logout, entry.EventType);
        Assert.Equal(UserId, entry.UserId);
        Assert.Equal("jsmith", entry.Username);
        Assert.Equal(UserId, entry.CreatedBy);
        Assert.Empty(h.Actor.Operations);
    }

    [Fact]
    public async Task Logout_of_an_unknown_token_succeeds_without_writes()
    {
        var h = new Harness();
        h.Sessions.Session = null;

        var result = await h.Service.LogoutAsync(new LogoutRequest { RefreshToken = h.Tokens.Create().RawToken }, Client);

        Assert.True(result.IsSuccess);
        Assert.Empty(h.Sessions.Revocations);
        Assert.Empty(h.History.Entries);
        Assert.DoesNotContain("BEGIN", h.Log);
    }

    [Fact]
    public async Task Logout_of_an_already_revoked_session_succeeds_without_writes()
    {
        var h = new Harness();
        var session = ActiveSession();
        session.RevokedDate = DateTime.UtcNow;
        session.RevokedReason = SessionRevokedReasons.Rotated;
        h.Sessions.Session = session;

        var result = await h.Service.LogoutAsync(new LogoutRequest { RefreshToken = h.Tokens.Create().RawToken }, Client);

        Assert.True(result.IsSuccess);
        Assert.Empty(h.Sessions.Revocations);
        Assert.Empty(h.History.Entries);
    }

    [Fact]
    public async Task Logout_of_a_malformed_token_succeeds_without_a_lookup()
    {
        var h = new Harness();

        var result = await h.Service.LogoutAsync(new LogoutRequest { RefreshToken = "garbage" }, Client);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("N4", h.Log);
    }

    #endregion

    [Fact]
    public void The_hash_stored_for_a_session_is_SHA256_of_the_raw_token_bytes()
    {
        var factory = new SessionTokenFactory();
        var token = factory.Create();

        var raw = System.Buffers.Text.Base64Url.DecodeFromChars(token.RawToken);

        Assert.Equal(SHA256.HashData(raw), token.Hash);
    }
}
