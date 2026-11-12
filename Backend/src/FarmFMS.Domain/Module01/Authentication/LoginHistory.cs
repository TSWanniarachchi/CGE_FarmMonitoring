/*
 * Description  : Login event history (table LoginHistory).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authentication;

/// <summary>Login event history (table LoginHistory).</summary>
public class LoginHistory : FarmFMS.Domain.Common.AuditableEntity
{
    /// <summary>Null when the username is unknown.</summary>
    public int? UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>TINYINT; values of <see cref="LoginEventType"/>.</summary>
    public byte EventType { get; set; }

    /// <summary>Event time, UTC.</summary>
    public DateTime EventDate { get; set; }

    public string? IPAddress { get; set; }

    public string? DeviceInfo { get; set; }

    /// <summary>One of <see cref="LoginFailureReasons"/>.</summary>
    public string? FailureReason { get; set; }
}
