using System.ComponentModel.DataAnnotations;

/*
 * Description  : Logout request (Step 0 spec section 6.4).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>Refresh token of the session to end, sent to <c>POST api/auth/logout</c>.</summary>
public sealed class LogoutRequest
{
    [Required(ErrorMessage = "RefreshToken is required.")]
    [StringLength(SessionPolicy.RefreshTokenLength, MinimumLength = SessionPolicy.RefreshTokenLength,
        ErrorMessage = "RefreshToken is not valid.")]
    public string RefreshToken { get; set; } = string.Empty;
}
