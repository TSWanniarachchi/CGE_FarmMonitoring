/*
 * Description  : Restricted credential retrieval for login (read-only, D-03): returns the AppUser row with its
 *                encrypted password payload and the UserAccount platform flags. The API decrypts and verifies
 *                (AES-256-GCM, AD-11); no outcome is written here. 0 rows = unknown username.
 *                Username matching relies on the database's case-insensitive collation; the caller trims and
 *                NFC-normalizes the value (D-21).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Farm_FMS_Module_01_Database_Specification_v0.2 (Stored Procedures: SvaAppUserLogin);
 *                Step00_Authentication_Foundation_Spec section 5.1 (D-02, D-03, D-21)
 */
CREATE OR ALTER PROCEDURE dbo.SvaAppUserLogin
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT
        u.Id          AS UserId,
        u.Username,
        u.FirstName,
        u.LastName,
        u.EncPassword,
        u.PasswordNonce,
        u.PasswordTag,
        u.KeyVersion,
        u.IsActive,
        u.IsLocked,
        u.FailedLogCount,
        u.PwdChangeReq,
        a.Id          AS UserAccountId,
        a.IsActive    AS AccountIsActive,
        a.CanAccessWeb,
        a.CanAccessMobile
    FROM dbo.AppUser AS u
    LEFT JOIN dbo.UserAccount AS a
        ON a.UserId = u.Id
    WHERE u.Username = @Username;
END
