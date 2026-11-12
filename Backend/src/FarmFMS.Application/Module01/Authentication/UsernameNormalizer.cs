using System.Text;

/*
 * Description  : Username normalization policy (D-21).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// D-21: trim and Unicode NFC; no lower-casing. Case-insensitive matching relies on a case-insensitive database
/// collation (deployment check); the username is stored as entered.
/// </summary>
internal static class UsernameNormalizer
{
    /// <summary>The normalized username, or an empty string when the input is blank or not valid Unicode.</summary>
    public static string Normalize(string? username)
    {
        if (string.IsNullOrWhiteSpace(username)) return string.Empty;

        try
        {
            return username.Trim().Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }
}
