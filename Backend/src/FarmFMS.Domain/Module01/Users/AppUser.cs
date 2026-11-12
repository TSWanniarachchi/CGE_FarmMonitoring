/*
 * Description  : Application user (table AppUser, Module 01 specification v0.2). Password fields hold the encrypted value, nonce, tag and key version (AD-11).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Users;

/// <summary>Application user (table AppUser, Module 01 specification v0.2). Password fields hold the encrypted value, nonce, tag and key version (AD-11).</summary>
public class AppUser : FarmFMS.Domain.Common.AuditableEntity
{
    public string Username { get; set; } = string.Empty;

    /// <summary>Encrypted password (VARBINARY).</summary>
    public byte[] EncPassword { get; set; } = [];

    public byte[] PasswordNonce { get; set; } = [];

    public byte[] PasswordTag { get; set; } = [];

    public string KeyVersion { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? MobileNo { get; set; }

    public bool IsActive { get; set; }

    public int FailedLogCount { get; set; }

    public bool IsLocked { get; set; }

    /// <summary>Time the account was locked, UTC.</summary>
    public DateTime? LockedDate { get; set; }

    /// <summary>Last successful login, UTC.</summary>
    public DateTime? LastLoginDate { get; set; }

    /// <summary>Password change required at next login.</summary>
    public bool PwdChangeReq { get; set; }
}
