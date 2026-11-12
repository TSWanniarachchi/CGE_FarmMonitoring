using FarmFMS.Domain.Module01.Authorization;

/*
 * Description  : Business constants of the authentication flow (Step 0 spec section 6.2, D-06, D-13).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Business constants from the Module 01 specification (<i>Confirmed Decisions</i>); not configurable (D-06).
/// Technical values (token lifetimes, touch interval) come from options.
/// </summary>
public static class SessionPolicy
{
    /// <summary>Inactivity timeout enforced server-side (spec: 30 minutes).</summary>
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);

    public static int IdleTimeoutMinutes => (int)IdleTimeout.TotalMinutes;

    /// <summary>Consecutive failed passwords that lock the account (spec: 5).</summary>
    public const int MaxFailedLoginAttempts = 5;

    /// <summary>Every Step 0 login is a Web login (D-13); equals <see cref="MenuPlatform.Web"/>.</summary>
    public const byte PlatformWeb = (byte)MenuPlatform.Web;

    /// <summary>Length of a refresh token: 32 random bytes as base64url (D-24).</summary>
    public const int RefreshTokenLength = 43;
}
