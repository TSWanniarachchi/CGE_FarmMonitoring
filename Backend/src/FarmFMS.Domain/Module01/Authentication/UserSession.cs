/*
 * Description  : Refresh-token session (table UserSession). Only the token hash is stored.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authentication;

/// <summary>Refresh-token session (table UserSession). Only the token hash is stored.</summary>
public class UserSession : FarmFMS.Domain.Common.AuditableEntity
{
    public int UserId { get; set; }

    /// <summary>SHA-256 hash of the refresh token (VARBINARY(32)).</summary>
    public byte[] TokenHash { get; set; } = [];

    /// <summary>Last activity, UTC.</summary>
    public DateTime LastActDate { get; set; }

    /// <summary>Expiry, UTC.</summary>
    public DateTime ExpiresDate { get; set; }

    /// <summary>Revocation time, UTC.</summary>
    public DateTime? RevokedDate { get; set; }

    /// <summary>One of <see cref="SessionRevokedReasons"/>.</summary>
    public string? RevokedReason { get; set; }

    public string? IPAddress { get; set; }

    public string? DeviceInfo { get; set; }
}
