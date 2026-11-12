using System.Data;
using Dapper;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Domain.Module01;
using FarmFMS.Domain.Module01.Authentication;
using FarmFMS.Infrastructure.Data;

/*
 * Description  : Login history repository (ScrLoginHist).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Repositories.Module01;

internal sealed class LoginHistoryRepository : ILoginHistoryRepository
{
    private readonly IStoredProcedureExecutor _executor;

    public LoginHistoryRepository(IStoredProcedureExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public Task<int> CreateAsync(
        int? userId,
        string username,
        LoginEventType eventType,
        string? ipAddress,
        string? deviceInfo,
        string? failureReason,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Int32);
        parameters.Add("Username", username, DbType.String, size: Module01FieldLengths.Username);
        parameters.Add("EventType", (byte)eventType, DbType.Byte);
        parameters.Add("IPAddress", ipAddress, DbType.String, size: Module01FieldLengths.IPAddress);
        parameters.Add("DeviceInfo", deviceInfo, DbType.String, size: Module01FieldLengths.DeviceInfo);
        parameters.Add("FailureReason", failureReason, DbType.String, size: Module01FieldLengths.FailureReason);
        parameters.Add("CreatedBy", createdBy, DbType.Int32);

        return _executor.ExecuteForNewIdAsync(Module01Procedures.ScrLoginHist, parameters, cancellationToken);
    }
}
