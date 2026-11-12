/*
 * Description  : Login event kind stored in LoginHistory.EventType (TINYINT).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authentication;

/// <summary>Login event kind stored in LoginHistory.EventType (TINYINT).</summary>
public enum LoginEventType : byte
{
    Success = 1,
    Failed = 2,
    Logout = 3,
}
