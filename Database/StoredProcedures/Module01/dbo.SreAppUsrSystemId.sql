/*
 * Description  : Resolves the SYSTEM user's AppUser.Id by its configured username (AD-14, D-19).
 *                Resolves ONLY an inactive user (IsActive = 0): the SYSTEM user can never log in (D-19), so an
 *                active account that happens to have this username never becomes the audit actor.
 *                Single-column result: Id, or no row when no such inactive user exists (read as NULL by ExecuteScalar).
 *                Returns no other column; never password data.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Change notes : 12-11-2026 (G6 L-6) added AND IsActive = 0.
 * Specification: Step00_Authentication_Foundation_Spec section 5.2 (N8, D-19)
 */
CREATE OR ALTER PROCEDURE dbo.SreAppUsrSystemId
    @Username NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT Id
    FROM dbo.AppUser
    WHERE Username = @Username
      AND IsActive = 0;
END
