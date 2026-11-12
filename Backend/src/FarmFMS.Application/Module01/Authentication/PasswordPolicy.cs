using System.Text;

/*
 * Description  : Password rules applied when a password is set (spec Confirmed Decisions, P-04).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Password policy: at least 8 characters with an uppercase letter, a lowercase letter, a digit and a special
/// character (Module 01 specification, <i>Confirmed Decisions</i>); at most 128 characters (P-04 recommendation,
/// at most 512 UTF-8 bytes so it fits <c>AppUser.EncPassword</c>). "Special" = not a letter, digit or whitespace.
/// Messages never echo the password.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>The violated rules as user-safe messages; empty when the password satisfies the policy.</summary>
    public static IReadOnlyList<string> Validate(string? password)
    {
        string normalized;
        try
        {
            normalized = (password ?? string.Empty).Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return ["Password contains invalid characters."];
        }

        var problems = new List<string>();

        if (normalized.Length < MinLength)
            problems.Add($"Password must be at least {MinLength} characters.");
        if (normalized.Length > MaxLength)
            problems.Add($"Password must not exceed {MaxLength} characters.");
        if (!normalized.Any(char.IsUpper))
            problems.Add("Password must contain an uppercase letter.");
        if (!normalized.Any(char.IsLower))
            problems.Add("Password must contain a lowercase letter.");
        if (!normalized.Any(char.IsDigit))
            problems.Add("Password must contain a digit.");
        if (!normalized.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
            problems.Add("Password must contain a special character.");

        return problems;
    }
}
