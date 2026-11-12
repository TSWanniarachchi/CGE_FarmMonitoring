using System.Data;
using Dapper;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Domain.Module01;
using FarmFMS.Infrastructure.Data;

/*
 * Description  : Login repository (SvaAppUserLogin, SupAppUsrLoginSuccess, SupAppUsrLoginFailure).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Repositories.Module01;

internal sealed class LoginRepository : ILoginRepository
{
    private readonly IStoredProcedureExecutor _executor;

    public LoginRepository(IStoredProcedureExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public Task<LoginCredentialReadModel?> GetCredentialsAsync(string username, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Username", username, DbType.String, size: Module01FieldLengths.Username);

        return _executor.QuerySingleOrDefaultAsync<LoginCredentialReadModel>(
            Module01Procedures.SvaAppUserLogin, parameters, cancellationToken);
    }

    public Task<int> RecordLoginSuccessAsync(int userId, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Int32);

        return _executor.ExecuteForRowsAffectedAsync(
            Module01Procedures.SupAppUsrLoginSuccess, parameters, cancellationToken);
    }

    public async Task<LoginFailureOutcome> RecordLoginFailureAsync(
        int userId,
        int maxFailedAttempts,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Int32);
        parameters.Add("MaxFailedAttempts", maxFailedAttempts, DbType.Int32);
        parameters.Add("IsLocked", dbType: DbType.Boolean, direction: ParameterDirection.Output);

        var rows = await _executor.ExecuteForRowsAffectedAsync(
            Module01Procedures.SupAppUsrLoginFailure, parameters, cancellationToken);

        return new LoginFailureOutcome(rows, parameters.Get<bool?>("IsLocked") ?? false);
    }
}
