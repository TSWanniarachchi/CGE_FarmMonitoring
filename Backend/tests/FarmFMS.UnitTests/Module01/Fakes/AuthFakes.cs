using System.Data;
using FarmFMS.Application.Abstractions.Data;
using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Common.Exceptions;
using FarmFMS.Application.Common.Identity;
using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Application.Module01.Users;
using FarmFMS.Domain.Module01.Authentication;
using Microsoft.Extensions.Logging;

namespace FarmFMS.UnitTests.Module01.Fakes;

/// <summary>Ordered journal of the calls the fakes receive.</summary>
internal sealed class CallLog : List<string>
{
    public bool HappenedInOrder(params string[] expected)
    {
        var index = 0;
        foreach (var entry in this)
        {
            if (index < expected.Length && entry == expected[index]) index++;
        }

        return index == expected.Length;
    }
}

internal sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));

    public bool Contains(LogLevel level, string text) =>
        Entries.Any(e => e.Level == level && e.Message.Contains(text, StringComparison.Ordinal));

    public string All => string.Join('\n', Entries.Select(e => $"{e.Level}: {e.Message}"));
}

internal sealed class FakeLoginRepository(CallLog log) : ILoginRepository
{
    public LoginCredentialReadModel? Credentials { get; set; }
    public int SuccessRows { get; set; } = 1;
    public LoginFailureOutcome FailureOutcome { get; set; } = new(1, false);
    public string? LastUsername { get; private set; }
    public int? LastMaxFailedAttempts { get; private set; }
    public int CredentialLookups { get; private set; }

    public Task<LoginCredentialReadModel?> GetCredentialsAsync(string username, CancellationToken cancellationToken = default)
    {
        log.Add("GetCredentials");
        CredentialLookups++;
        LastUsername = username;
        return Task.FromResult(Credentials);
    }

    public Task<int> RecordLoginSuccessAsync(int userId, CancellationToken cancellationToken = default)
    {
        log.Add("N2");
        return Task.FromResult(SuccessRows);
    }

    public Task<LoginFailureOutcome> RecordLoginFailureAsync(int userId, int maxFailedAttempts, CancellationToken cancellationToken = default)
    {
        log.Add("N3");
        LastMaxFailedAttempts = maxFailedAttempts;
        return Task.FromResult(FailureOutcome);
    }
}

internal sealed record RevokeCall(int UserId, int? SessionId, string Reason, int ModifiedBy);

internal sealed record CreateSessionCall(int UserId, byte[] TokenHash, int AbsoluteLifetimeMinutes, string? IpAddress, string? DeviceInfo, int CreatedBy);

internal sealed record RotateCall(int CurrentSessionId, byte[] NewTokenHash, int IdleTimeoutMinutes, byte Platform, string? IpAddress, string? DeviceInfo, int CreatedBy);

internal sealed record ValidateCall(int SessionId, int UserId, int IdleTimeoutMinutes, int TouchIntervalSeconds, byte Platform, int ModifiedBy);

internal sealed class FakeUserSessionRepository(CallLog log) : IUserSessionRepository
{
    public CreatedSessionReadModel Created { get; set; } = new(501, new DateTime(2026, 11, 13, 6, 0, 0, DateTimeKind.Unspecified));
    public Exception? CreateException { get; set; }
    public SessionReadModel? Session { get; set; }
    public int RotatedSessionId { get; set; } = 502;
    public Exception? RotateException { get; set; }
    public SessionValidationStatus Status { get; set; } = SessionValidationStatus.Valid;
    public int RevokedCount { get; set; } = 3;
    public Exception? ValidateException { get; set; }

    /// <summary>When set, the second and later lookups by token hash return this (the row as it is after N5 refused).</summary>
    public SessionReadModel? SessionOnLaterReads { get; set; }
    public bool UseSessionOnLaterReads { get; set; }
    public int Lookups { get; private set; }

    public List<CreateSessionCall> Creates { get; } = [];
    public List<RotateCall> Rotations { get; } = [];
    public List<ValidateCall> Validations { get; } = [];
    public List<RevokeCall> Revocations { get; } = [];
    public byte[]? LookedUpHash { get; private set; }

    public Task<CreatedSessionReadModel> CreateAsync(int userId, byte[] tokenHash, int absoluteLifetimeMinutes, string? ipAddress, string? deviceInfo, int createdBy, CancellationToken cancellationToken = default)
    {
        log.Add("N1");
        if (CreateException is not null) throw CreateException;
        Creates.Add(new CreateSessionCall(userId, tokenHash, absoluteLifetimeMinutes, ipAddress, deviceInfo, createdBy));
        return Task.FromResult(Created);
    }

    public Task<SessionReadModel?> GetByTokenHashAsync(byte[] tokenHash, int idleTimeoutMinutes, CancellationToken cancellationToken = default)
    {
        log.Add("N4");
        LookedUpHash = tokenHash;
        Lookups++;
        return Task.FromResult(UseSessionOnLaterReads && Lookups >= 2 ? SessionOnLaterReads : Session);
    }

    public Task<int> RotateAsync(int currentSessionId, byte[] newTokenHash, int idleTimeoutMinutes, byte platform, string? ipAddress, string? deviceInfo, int createdBy, CancellationToken cancellationToken = default)
    {
        log.Add("N5");
        if (RotateException is not null) throw RotateException;
        Rotations.Add(new RotateCall(currentSessionId, newTokenHash, idleTimeoutMinutes, platform, ipAddress, deviceInfo, createdBy));
        return Task.FromResult(RotatedSessionId);
    }

    public Task<SessionValidationStatus> ValidateAsync(int sessionId, int userId, int idleTimeoutMinutes, int touchIntervalSeconds, byte platform, int modifiedBy, CancellationToken cancellationToken = default)
    {
        log.Add("N6");
        if (ValidateException is not null) throw ValidateException;
        Validations.Add(new ValidateCall(sessionId, userId, idleTimeoutMinutes, touchIntervalSeconds, platform, modifiedBy));
        return Task.FromResult(Status);
    }

    public Task<int> RevokeAsync(int userId, int? sessionId, string revokedReason, int modifiedBy, CancellationToken cancellationToken = default)
    {
        log.Add("Revoke");
        Revocations.Add(new RevokeCall(userId, sessionId, revokedReason, modifiedBy));
        return Task.FromResult(RevokedCount);
    }
}

internal sealed record HistoryEntry(int? UserId, string Username, LoginEventType EventType, string? IpAddress, string? DeviceInfo, string? FailureReason, int CreatedBy);

internal sealed class FakeLoginHistoryRepository(CallLog log) : ILoginHistoryRepository
{
    public List<HistoryEntry> Entries { get; } = [];

    public Task<int> CreateAsync(int? userId, string username, LoginEventType eventType, string? ipAddress, string? deviceInfo, string? failureReason, int createdBy, CancellationToken cancellationToken = default)
    {
        log.Add("History");
        Entries.Add(new HistoryEntry(userId, username, eventType, ipAddress, deviceInfo, failureReason, createdBy));
        return Task.FromResult(Entries.Count);
    }
}

internal sealed class FakePasswordProtector : IPasswordProtector
{
    public PasswordVerificationResult Result { get; set; } = PasswordVerificationResult.Match;
    public int VerifyCalls { get; private set; }
    public List<(string Password, int UserId)> Protected { get; } = [];
    public int Placeholders { get; private set; }

    public PasswordVerificationResult Verify(string password, EncryptedPassword payload, int userId)
    {
        VerifyCalls++;
        return Result;
    }

    public EncryptedPassword Protect(string password, int userId)
    {
        Protected.Add((password, userId));
        return new EncryptedPassword([1, 2, 3], new byte[12], new byte[16], "fake-1");
    }

    public EncryptedPassword CreatePlaceholder()
    {
        Placeholders++;
        return new EncryptedPassword([9, 9, 9], new byte[12], new byte[16], "fake-1");
    }
}

internal sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
{
    public List<(int UserId, int SessionId, string Username)> Issued { get; } = [];

    public AccessToken Issue(int userId, int sessionId, string username)
    {
        Issued.Add((userId, sessionId, username));
        return new AccessToken($"access-token-{sessionId}", new DateTime(2026, 11, 12, 6, 10, 0, DateTimeKind.Utc));
    }
}

internal sealed class FakeSessionSettings : ISessionSettings
{
    public int AbsoluteLifetimeMinutes { get; set; } = 720;
    public int ActivityTouchIntervalSeconds { get; set; } = 60;
}

/// <summary>
/// Audit actor fake: the System user Id is returned only inside <c>BeginSystemOperation</c>, otherwise the actor is
/// "required" (as for an unauthenticated request) - this proves that unauthenticated writes use the System scope.
/// </summary>
internal sealed class FakeAuditActor : IAuditActorService
{
    private int _depth;

    public int? SystemUserId { get; set; } = 7;
    public List<string> Operations { get; } = [];

    public Task<Result<int>> GetActorIdAsync(CancellationToken cancellationToken = default)
    {
        if (_depth > 0)
        {
            return Task.FromResult<Result<int>>(SystemUserId is { } id
                ? id
                : AuditErrors.SystemUserNotConfigured);
        }

        return Task.FromResult<Result<int>>(AuditErrors.ActorRequired);
    }

    public IDisposable BeginSystemOperation(string operationName)
    {
        Operations.Add(operationName);
        _depth++;
        return new Scope(this);
    }

    private sealed class Scope(FakeAuditActor owner) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            owner._depth--;
        }
    }
}

/// <summary>Unit of work fake with the real commit / rollback rules of <c>ExecuteInTransactionAsync</c>.</summary>
internal sealed class FakeUnitOfWork(CallLog log) : IUnitOfWork
{
    public bool HasActiveTransaction { get; private set; }
    public int Commits { get; private set; }
    public int Rollbacks { get; private set; }

    public Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task CommitAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public Task RollbackAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
        where TResult : Result
    {
        log.Add("BEGIN");
        HasActiveTransaction = true;
        try
        {
            var result = await operation(cancellationToken);
            if (result.IsSuccess)
            {
                Commits++;
                log.Add("COMMIT");
            }
            else
            {
                Rollbacks++;
                log.Add("ROLLBACK");
            }

            return result;
        }
        catch
        {
            Rollbacks++;
            log.Add("ROLLBACK");
            throw;
        }
        finally
        {
            HasActiveTransaction = false;
        }
    }
}

internal sealed class FakeUserPermissionRepository : IUserPermissionRepository
{
    public List<EffectivePermissionReadModel> Rows { get; } = [];
    public int Calls { get; private set; }
    public List<(int UserId, byte Platform)> Requests { get; } = [];
    public Exception? ExceptionToThrow { get; set; }
    public CancellationToken LastToken { get; private set; }

    public Task<IReadOnlyList<EffectivePermissionReadModel>> GetEffectivePermissionsAsync(int userId, byte platform, CancellationToken cancellationToken = default)
    {
        Calls++;
        LastToken = cancellationToken;
        Requests.Add((userId, platform));
        if (ExceptionToThrow is not null) throw ExceptionToThrow;
        return Task.FromResult<IReadOnlyList<EffectivePermissionReadModel>>(Rows.ToList());
    }
}

internal sealed class FakeAppUserReadRepository : IAppUserReadRepository
{
    public AppUserProfileReadModel? User { get; set; }

    public Task<AppUserProfileReadModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(User);
}

internal sealed class FakeBootstrapRepository(CallLog log) : IAdministratorBootstrapRepository
{
    public int? ExistingSystemUserId { get; set; }
    public int NextId { get; set; } = 100;
    public int PasswordRows { get; set; } = 1;
    public Exception? CreateUserExceptionForAdministrator { get; set; }

    public List<BootstrapUserData> Users { get; } = [];
    public List<(int UserId, EncryptedPassword Payload, bool PwdChangeReq, int ModifiedBy)> Passwords { get; } = [];
    public List<(int UserId, int ProcessId, bool Web, bool Mobile, int CreatedBy)> Accounts { get; } = [];
    public string? LookedUpUsername { get; private set; }
    public int LookupCalls { get; private set; }

    public Task<int?> GetSystemUserIdAsync(string username, CancellationToken cancellationToken = default)
    {
        log.Add("N8");
        LookupCalls++;
        LookedUpUsername = username;
        return Task.FromResult(ExistingSystemUserId);
    }

    public Task<int> CreateUserAsync(BootstrapUserData user, CancellationToken cancellationToken = default)
    {
        log.Add(user.IsActive ? "ScrAppUser:admin" : "ScrAppUser:system");
        if (user.IsActive && CreateUserExceptionForAdministrator is not null)
            throw CreateUserExceptionForAdministrator;

        Users.Add(user);
        return Task.FromResult(NextId++);
    }

    public Task<int> SetPasswordAsync(int userId, EncryptedPassword payload, bool passwordChangeRequired, int modifiedBy, CancellationToken cancellationToken = default)
    {
        log.Add("N7");
        Passwords.Add((userId, payload, passwordChangeRequired, modifiedBy));
        return Task.FromResult(PasswordRows);
    }

    public Task<int> CreateUserAccountAsync(int userId, int processId, bool canAccessWeb, bool canAccessMobile, int createdBy, CancellationToken cancellationToken = default)
    {
        log.Add("ScrUserAccount");
        Accounts.Add((userId, processId, canAccessWeb, canAccessMobile, createdBy));
        return Task.FromResult(NextId++);
    }
}

internal sealed class FakeSystemUserSettings(string username = "SYSTEM") : ISystemUserSettings
{
    public string Username { get; } = username;
}

internal static class Errors
{
    public static DataAccessException Database(int number, Error error) => new(error, number, isTransient: false);
}
