using System.Data;
using Dapper;
using FarmFMS.Application.Module01.Users;
using FarmFMS.Infrastructure.Data;

/*
 * Description  : User read repository (SreAppUserById). Never reads password columns.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Repositories.Module01;

internal sealed class AppUserReadRepository : IAppUserReadRepository
{
    private readonly IStoredProcedureExecutor _executor;

    public AppUserReadRepository(IStoredProcedureExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public Task<AppUserProfileReadModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Id", id, DbType.Int32);

        return _executor.QuerySingleOrDefaultAsync<AppUserProfileReadModel>(
            Module01Procedures.SreAppUserById, parameters, cancellationToken);
    }
}
