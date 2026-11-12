using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;

/*
 * Description  : Development-only first Administrator bootstrap contracts (Step 0 spec section 7.4, D-19, D-20).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Users;

/// <summary>
/// Data access for the bootstrap seed: <c>SreAppUsrSystemId</c> (N8), <c>ScrAppUser</c>, <c>SupAppUsrPwd</c> (N7)
/// and <c>ScrUserAccount</c>. One method per Stored Procedure.
/// </summary>
public interface IAdministratorBootstrapRepository
{
    /// <summary><c>dbo.SreAppUsrSystemId</c> (N8): the Id of the user with this username, or <c>null</c>.</summary>
    Task<int?> GetSystemUserIdAsync(string username, CancellationToken cancellationToken = default);

    /// <summary><c>dbo.ScrAppUser</c>: returns the new Id. Raises 50005 when the username is taken.</summary>
    Task<int> CreateUserAsync(BootstrapUserData user, CancellationToken cancellationToken = default);

    /// <summary><c>dbo.SupAppUsrPwd</c> (N7): rows affected (0 = user not found).</summary>
    Task<int> SetPasswordAsync(
        int userId,
        EncryptedPassword payload,
        bool passwordChangeRequired,
        int modifiedBy,
        CancellationToken cancellationToken = default);

    /// <summary><c>dbo.ScrUserAccount</c>: returns the new Id. Raises 50004 / 50006.</summary>
    Task<int> CreateUserAccountAsync(
        int userId,
        int processId,
        bool canAccessWeb,
        bool canAccessMobile,
        int createdBy,
        CancellationToken cancellationToken = default);
}

/// <summary>Input of <c>dbo.ScrAppUser</c> for the bootstrap seed.</summary>
public sealed record BootstrapUserData(
    string Username,
    string FirstName,
    string? LastName,
    string? Email,
    EncryptedPassword Payload,
    bool PasswordChangeRequired,
    bool IsActive,
    int CreatedBy);

/// <summary>
/// Technical name of the dedicated SYSTEM user (AD-14, D-19). Implemented by Infrastructure from the
/// <c>SystemUser:Username</c> option (default <c>SYSTEM</c>).
/// </summary>
public interface ISystemUserSettings
{
    string Username { get; }
}

/// <summary>Input of the bootstrap. The password is never logged; <see cref="ProcessId"/> is supplied by the operator.</summary>
public sealed class AdministratorBootstrapRequest
{
    public string Username { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string Password { get; init; } = string.Empty;

    /// <summary>Process Id of the administrator's <c>UserAccount</c> (NOT NULL; never hard-coded, D-20 / C-09). Must be greater than 0.</summary>
    public int ProcessId { get; init; }

    /// <summary>Sets <c>AppUser.PwdChangeReq</c>. Default false (informational only in Step 0, D-15).</summary>
    public bool PasswordChangeRequired { get; init; }
}

/// <summary>Ids created or resolved by the bootstrap.</summary>
public sealed record AdministratorBootstrapResult(int AdministratorId, int SystemUserId, bool SystemUserCreated);

/// <summary>
/// Creates, in ONE unit of work, the SYSTEM user (when missing) and the first Administrator with a web-enabled
/// <c>UserAccount</c>. No roles are seeded (DBG-014: the specification defines no Role / Permission / Menu procedures).
/// Development only (D-31); executing it is a database change that needs explicit user approval.
/// </summary>
public interface IAdministratorBootstrapService
{
    Task<Result<AdministratorBootstrapResult>> BootstrapAsync(
        AdministratorBootstrapRequest request,
        CancellationToken cancellationToken = default);
}
