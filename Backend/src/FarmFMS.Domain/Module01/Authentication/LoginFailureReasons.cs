/*
 * Description  : Values for LoginHistory.FailureReason (D-17).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authentication;

/// <summary>Values for LoginHistory.FailureReason (D-17).</summary>
public static class LoginFailureReasons
{
    public const string UnknownUser = "UnknownUser";
    public const string Inactive = "Inactive";
    public const string Locked = "Locked";
    public const string InvalidPassword = "InvalidPassword";
    public const string CredentialError = "CredentialError";
    public const string PlatformDenied = "PlatformDenied";
}
