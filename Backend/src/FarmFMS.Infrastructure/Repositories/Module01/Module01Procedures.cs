/*
 * Description  : dbo-qualified Stored Procedure names of Module 01 (Security & Access Management).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Repositories.Module01;

/// <summary>
/// Procedure names used by the Module 01 repositories. Scripts: <c>Database/StoredProcedures/Module01/dbo.&lt;Name&gt;.sql</c>.
/// Specification names are preserved exactly (incl. <c>User</c>); procedures added by the Step 0 specification
/// (section 5.2, N1-N8) use <c>Usr</c>. Step 0: Step00_Authentication_Foundation_Spec section 5.
/// </summary>
public static class Module01Procedures
{
    #region Specification procedures (Module 01 database specification v0.2)

    public const string ScrAppUser = "dbo.ScrAppUser";
    public const string ScrUserAccount = "dbo.ScrUserAccount";
    public const string SreAppUserById = "dbo.SreAppUserById";
    public const string SvaAppUserLogin = "dbo.SvaAppUserLogin";
    public const string SvaUsrPerm = "dbo.SvaUsrPerm";
    public const string ScrLoginHist = "dbo.ScrLoginHist";
    public const string SupUserSessRev = "dbo.SupUserSessRev";

    #endregion

    #region Step 0 additions (DBG-012; N1-N8)

    /// <summary>N1: create a session at login.</summary>
    public const string ScrUsrSess = "dbo.ScrUsrSess";

    /// <summary>N2: reset the failure count and set LastLoginDate.</summary>
    public const string SupAppUsrLoginSuccess = "dbo.SupAppUsrLoginSuccess";

    /// <summary>N3: atomic failure count and lockout.</summary>
    public const string SupAppUsrLoginFailure = "dbo.SupAppUsrLoginFailure";

    /// <summary>N4: read a session by refresh-token hash.</summary>
    public const string SreUsrSessByTokenHash = "dbo.SreUsrSessByTokenHash";

    /// <summary>N5: rotate a refresh token.</summary>
    public const string ScrUsrSessRotate = "dbo.ScrUsrSessRotate";

    /// <summary>N6: per-request session validation.</summary>
    public const string SvaUsrSess = "dbo.SvaUsrSess";

    /// <summary>N7: set the encrypted password payload (bootstrap seed only in Step 0).</summary>
    public const string SupAppUsrPwd = "dbo.SupAppUsrPwd";

    /// <summary>N8: resolve the SYSTEM user's Id.</summary>
    public const string SreAppUsrSystemId = "dbo.SreAppUsrSystemId";

    #endregion
}
