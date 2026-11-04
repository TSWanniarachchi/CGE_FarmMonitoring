using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Common.Results;
using Microsoft.Extensions.Logging;

/*
 * Description  : Resolves the user ID recorded in CreatedBy / ModifiedBy (authenticated user or System user).
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Application.Common.Identity;

/// <summary>
/// Scoped implementation of <see cref="IAuditActorService"/>. See the interface for the resolution rules.
/// </summary>
internal sealed class AuditActorService : IAuditActorService
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISystemUserProvider _systemUserProvider;
    private readonly ILogger<AuditActorService> _logger;

    private int _systemOperationDepth;

    public AuditActorService(
        ICurrentUserService currentUser,
        ISystemUserProvider systemUserProvider,
        ILogger<AuditActorService> logger)
    {
        _currentUser = currentUser ??
            throw new ArgumentNullException(nameof(currentUser));
        _systemUserProvider = systemUserProvider ??
            throw new ArgumentNullException(nameof(systemUserProvider));
        _logger = logger ??
            throw new ArgumentNullException(nameof(logger));
    }

    private bool IsSystemOperation => Volatile.Read(ref _systemOperationDepth) > 0;

    public async Task<Result<int>> GetActorIdAsync(CancellationToken cancellationToken = default)
    {
        if (IsSystemOperation)
        {
            var systemUserId = await _systemUserProvider.GetSystemUserIdAsync(cancellationToken);
            if (systemUserId is > 0)
                return systemUserId.Value;

            _logger.LogError("A system operation needs the System user ID, but it is not configured (SystemUser:UserId)");
            return AuditErrors.SystemUserNotConfigured;
        }

        if (_currentUser.IsAuthenticated && _currentUser.UserId is > 0)
            return _currentUser.UserId.Value;

        return AuditErrors.ActorRequired;
    }

    public IDisposable BeginSystemOperation(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        Interlocked.Increment(ref _systemOperationDepth);
        _logger.LogInformation("System operation {OperationName} started", operationName);

        return new SystemOperationScope(this, operationName);
    }

    private void EndSystemOperation(string operationName)
    {
        Interlocked.Decrement(ref _systemOperationDepth);
        _logger.LogInformation("System operation {OperationName} ended", operationName);
    }

    private sealed class SystemOperationScope(AuditActorService owner, string operationName) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.EndSystemOperation(operationName);
        }
    }
}
