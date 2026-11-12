/*
 * Description  : Values for UserSession.RevokedReason (D-07).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authentication;

/// <summary>Values for UserSession.RevokedReason (D-07).</summary>
public static class SessionRevokedReasons
{
    public const string Logout = "Logout";
    public const string Rotated = "Rotated";
    public const string RefreshTokenReuse = "RefreshTokenReuse";
    public const string IdleTimeout = "IdleTimeout";
    public const string Expired = "Expired";
    public const string AccountState = "AccountState";
}
