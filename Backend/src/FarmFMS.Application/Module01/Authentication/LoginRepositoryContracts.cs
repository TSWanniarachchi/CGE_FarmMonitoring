/*
 * Description  : Login repository contract and read model (SvaAppUserLogin, N2, N3).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Login credential data access. One method per Stored Procedure (spec section 5).
/// </summary>
public interface ILoginRepository
{
    /// <summary><c>dbo.SvaAppUserLogin</c>: read-only credential retrieval; <c>null</c> when the username is unknown (D-03).</summary>
    Task<LoginCredentialReadModel?> GetCredentialsAsync(string username, CancellationToken cancellationToken = default);

    /// <summary><c>dbo.SupAppUsrLoginSuccess</c> (N2): resets the failure count, sets <c>LastLoginDate</c>. Rows affected; 0 = user no longer active / unlocked.</summary>
    Task<int> RecordLoginSuccessAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary><c>dbo.SupAppUsrLoginFailure</c> (N3): atomic +1, locks at <paramref name="maxFailedAttempts"/>.</summary>
    Task<LoginFailureOutcome> RecordLoginFailureAsync(int userId, int maxFailedAttempts, CancellationToken cancellationToken = default);
}

/// <summary>Result of N3: rows affected (0 = user not active / already locked) and whether the account is now locked.</summary>
public sealed record LoginFailureOutcome(int RowsAffected, bool IsLocked);

/// <summary>
/// The <c>SvaAppUserLogin</c> row: the user, the encrypted password payload and the platform access of the user account
/// (the account columns are null when the user has no <c>UserAccount</c>).
/// </summary>
public sealed class LoginCredentialReadModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public byte[] EncPassword { get; set; } = [];
    public byte[] PasswordNonce { get; set; } = [];
    public byte[] PasswordTag { get; set; } = [];
    public string KeyVersion { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public int FailedLogCount { get; set; }
    public bool PwdChangeReq { get; set; }
    public int? UserAccountId { get; set; }
    public bool? AccountIsActive { get; set; }
    public bool? CanAccessWeb { get; set; }
    public bool? CanAccessMobile { get; set; }
}
