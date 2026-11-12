using System.Data;
using Dapper;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Domain.Module01;
using FarmFMS.Infrastructure.Data;

/*
 * Description  : User session repository (ScrUsrSess, SreUsrSessByTokenHash, ScrUsrSessRotate, SvaUsrSess, SupUserSessRev).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Repositories.Module01;

internal sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly IStoredProcedureExecutor _executor;

    public UserSessionRepository(IStoredProcedureExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public async Task<CreatedSessionReadModel> CreateAsync(
        int userId,
        byte[] tokenHash,
        int absoluteLifetimeMinutes,
        string? ipAddress,
        string? deviceInfo,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Int32);
        parameters.Add("TokenHash", tokenHash, DbType.Binary, size: Module01FieldLengths.TokenHash);
        parameters.Add("AbsoluteLifetimeMinutes", absoluteLifetimeMinutes, DbType.Int32);
        parameters.Add("IPAddress", ipAddress, DbType.String, size: Module01FieldLengths.IPAddress);
        parameters.Add("DeviceInfo", deviceInfo, DbType.String, size: Module01FieldLengths.DeviceInfo);
        parameters.Add("CreatedBy", createdBy, DbType.Int32);
        parameters.Add("ExpiresDate", dbType: DbType.DateTime2, direction: ParameterDirection.Output);

        var sessionId = await _executor.ExecuteForNewIdAsync(Module01Procedures.ScrUsrSess, parameters, cancellationToken);

        var expires = parameters.Get<DateTime?>("ExpiresDate")
            ?? throw new InvalidOperationException(
                $"Stored procedure {Module01Procedures.ScrUsrSess} did not set @ExpiresDate.");

        return new CreatedSessionReadModel(sessionId, DateTime.SpecifyKind(expires, DateTimeKind.Utc));
    }

    public Task<SessionReadModel?> GetByTokenHashAsync(
        byte[] tokenHash,
        int idleTimeoutMinutes,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("TokenHash", tokenHash, DbType.Binary, size: Module01FieldLengths.TokenHash);
        parameters.Add("IdleTimeoutMinutes", idleTimeoutMinutes, DbType.Int32);

        return _executor.QuerySingleOrDefaultAsync<SessionReadModel>(
            Module01Procedures.SreUsrSessByTokenHash, parameters, cancellationToken);
    }

    public Task<int> RotateAsync(
        int currentSessionId,
        byte[] newTokenHash,
        int idleTimeoutMinutes,
        byte platform,
        string? ipAddress,
        string? deviceInfo,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("CurrentSessionId", currentSessionId, DbType.Int32);
        parameters.Add("NewTokenHash", newTokenHash, DbType.Binary, size: Module01FieldLengths.TokenHash);
        parameters.Add("IdleTimeoutMinutes", idleTimeoutMinutes, DbType.Int32);
        parameters.Add("Platform", platform, DbType.Byte);
        parameters.Add("IPAddress", ipAddress, DbType.String, size: Module01FieldLengths.IPAddress);
        parameters.Add("DeviceInfo", deviceInfo, DbType.String, size: Module01FieldLengths.DeviceInfo);
        parameters.Add("CreatedBy", createdBy, DbType.Int32);

        return _executor.ExecuteForNewIdAsync(Module01Procedures.ScrUsrSessRotate, parameters, cancellationToken);
    }

    public async Task<SessionValidationStatus> ValidateAsync(
        int sessionId,
        int userId,
        int idleTimeoutMinutes,
        int touchIntervalSeconds,
        byte platform,
        int modifiedBy,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("SessionId", sessionId, DbType.Int32);
        parameters.Add("UserId", userId, DbType.Int32);
        parameters.Add("IdleTimeoutMinutes", idleTimeoutMinutes, DbType.Int32);
        parameters.Add("TouchIntervalSeconds", touchIntervalSeconds, DbType.Int32);
        parameters.Add("Platform", platform, DbType.Byte);
        parameters.Add("ModifiedBy", modifiedBy, DbType.Int32);

        // byte? (not byte): a missing row must never read as 0 = Valid.
        var status = await _executor.QuerySingleOrDefaultAsync<byte?>(
            Module01Procedures.SvaUsrSess, parameters, cancellationToken);

        return status is { } value && Enum.IsDefined(typeof(SessionValidationStatus), (int)value)
            ? (SessionValidationStatus)value
            : SessionValidationStatus.NotFoundOrUserMismatch;
    }

    public Task<int> RevokeAsync(
        int userId,
        int? sessionId,
        string revokedReason,
        int modifiedBy,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Int32);
        parameters.Add("SessionId", sessionId, DbType.Int32);
        parameters.Add("RevokedReason", revokedReason, DbType.String, size: Module01FieldLengths.RevokedReason);
        parameters.Add("ModifiedBy", modifiedBy, DbType.Int32);

        return _executor.ExecuteForRowsAffectedAsync(Module01Procedures.SupUserSessRev, parameters, cancellationToken);
    }
}
