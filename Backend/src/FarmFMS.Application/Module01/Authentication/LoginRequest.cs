using System.ComponentModel.DataAnnotations;
using FarmFMS.Domain.Module01;

/*
 * Description  : Login request (Step 0 spec section 6.4).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Credentials sent to <c>POST api/auth/login</c>. <see cref="Password"/> is input-bound only (P-04: max 128);
/// no complexity rule is applied on login, the policy applies when a password is set.
/// </summary>
public sealed class LoginRequest
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(Module01FieldLengths.Username, ErrorMessage = "Username must not exceed {1} characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(PasswordPolicy.MaxLength, ErrorMessage = "Password must not exceed {1} characters.")]
    public string Password { get; set; } = string.Empty;
}
