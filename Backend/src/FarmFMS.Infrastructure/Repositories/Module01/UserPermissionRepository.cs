using System.Data;
using Dapper;
using FarmFMS.Application.Module01.Authorization;
using FarmFMS.Infrastructure.Data;

/*
 * Description  : Effective permission repository (SvaUsrPerm).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Repositories.Module01;

internal sealed class UserPermissionRepository : IUserPermissionRepository
{
    private readonly IStoredProcedureExecutor _executor;

    public UserPermissionRepository(IStoredProcedureExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public Task<IReadOnlyList<EffectivePermissionReadModel>> GetEffectivePermissionsAsync(
        int userId,
        byte platform,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Int32);
        parameters.Add("Platform", platform, DbType.Byte);

        return _executor.QueryAsync<EffectivePermissionReadModel>(
            Module01Procedures.SvaUsrPerm, parameters, cancellationToken);
    }
}
