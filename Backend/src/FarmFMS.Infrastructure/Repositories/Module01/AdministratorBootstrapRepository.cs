using System.Data;
using Dapper;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Users;
using FarmFMS.Domain.Module01;
using FarmFMS.Infrastructure.Data;

/*
 * Description  : Bootstrap / System user repository (SreAppUsrSystemId, ScrAppUser, SupAppUsrPwd, ScrUserAccount).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Repositories.Module01;

internal sealed class AdministratorBootstrapRepository : IAdministratorBootstrapRepository
{
    private readonly IStoredProcedureExecutor _executor;

    public AdministratorBootstrapRepository(IStoredProcedureExecutor executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public Task<int?> GetSystemUserIdAsync(string username, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("Username", username, DbType.String, size: Module01FieldLengths.Username);

        // int? (not int): no row must read as null, never as 0.
        return _executor.ExecuteScalarAsync<int?>(Module01Procedures.SreAppUsrSystemId, parameters, cancellationToken);
    }

    public Task<int> CreateUserAsync(BootstrapUserData user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var parameters = new DynamicParameters();
        parameters.Add("Username", user.Username, DbType.String, size: Module01FieldLengths.Username);
        parameters.Add("FirstName", user.FirstName, DbType.String, size: Module01FieldLengths.FirstName);
        parameters.Add("LastName", user.LastName, DbType.String, size: Module01FieldLengths.LastName);
        parameters.Add("Email", user.Email, DbType.String, size: Module01FieldLengths.Email);
        parameters.Add("MobileNo", null, DbType.String, size: Module01FieldLengths.MobileNo);
        AddPayload(parameters, user.Payload);
        parameters.Add("PwdChangeReq", user.PasswordChangeRequired, DbType.Boolean);
        parameters.Add("IsActive", user.IsActive, DbType.Boolean);
        parameters.Add("CreatedBy", user.CreatedBy, DbType.Int32);

        return _executor.ExecuteForNewIdAsync(Module01Procedures.ScrAppUser, parameters, cancellationToken);
    }

    public Task<int> SetPasswordAsync(
        int userId,
        EncryptedPassword payload,
        bool passwordChangeRequired,
        int modifiedBy,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Int32);
        AddPayload(parameters, payload);
        parameters.Add("PwdChangeReq", passwordChangeRequired, DbType.Boolean);
        parameters.Add("ModifiedBy", modifiedBy, DbType.Int32);

        return _executor.ExecuteForRowsAffectedAsync(Module01Procedures.SupAppUsrPwd, parameters, cancellationToken);
    }

    public Task<int> CreateUserAccountAsync(
        int userId,
        int processId,
        bool canAccessWeb,
        bool canAccessMobile,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("UserId", userId, DbType.Int32);
        parameters.Add("ProcessId", processId, DbType.Int32);
        parameters.Add("CanAccessWeb", canAccessWeb, DbType.Boolean);
        parameters.Add("CanAccessMobile", canAccessMobile, DbType.Boolean);
        parameters.Add("CreatedBy", createdBy, DbType.Int32);

        return _executor.ExecuteForNewIdAsync(Module01Procedures.ScrUserAccount, parameters, cancellationToken);
    }

    /// <summary>Binary columns are typed explicitly with the column sizes.</summary>
    private static void AddPayload(DynamicParameters parameters, EncryptedPassword payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        parameters.Add("EncPassword", payload.Ciphertext, DbType.Binary, size: Module01FieldLengths.EncPassword);
        parameters.Add("PasswordNonce", payload.Nonce, DbType.Binary, size: Module01FieldLengths.PasswordNonce);
        parameters.Add("PasswordTag", payload.Tag, DbType.Binary, size: Module01FieldLengths.PasswordTag);
        parameters.Add("KeyVersion", payload.KeyVersion, DbType.String, size: Module01FieldLengths.KeyVersion);
    }
}
