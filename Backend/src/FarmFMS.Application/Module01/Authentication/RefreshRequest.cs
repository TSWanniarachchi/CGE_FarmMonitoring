using System.ComponentModel.DataAnnotations;

/*
 * Description  : Refresh request (Step 0 spec section 6.4).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>Refresh token (32 random bytes, base64url = 43 characters) sent to <c>POST api/auth/refresh</c>.</summary>
public sealed class RefreshRequest
{
    [Required(ErrorMessage = "RefreshToken is required.")]
    [StringLength(SessionPolicy.RefreshTokenLength, MinimumLength = SessionPolicy.RefreshTokenLength,
        ErrorMessage = "RefreshToken is not valid.")]
    public string RefreshToken { get; set; } = string.Empty;
}
