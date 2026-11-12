/*
 * Description  : Login / refresh response (Step 0 spec section 6.4, D-15).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>Response of login and refresh. Property names are the single contract for the frontend (spec section 6.4).</summary>
public sealed class TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTime AccessTokenExpiresUtc { get; init; }

    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>Absolute end of the session (<c>UserSession.ExpiresDate</c>), UTC.</summary>
    public DateTime SessionExpiresUtc { get; init; }

    public int IdleTimeoutMinutes { get; init; }

    public string TokenType { get; init; } = "Bearer";

    /// <summary>From <c>AppUser.PwdChangeReq</c>; informational in Step 0 (D-15).</summary>
    public bool PasswordChangeRequired { get; init; }

    public AuthenticatedUserDto User { get; init; } = new();
}

/// <summary>The signed-in user as returned with a token response (no password fields).</summary>
public sealed class AuthenticatedUserDto
{
    public int Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string? LastName { get; init; }
}
