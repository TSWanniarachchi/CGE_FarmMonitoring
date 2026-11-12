/*
 * Description  : Login bookkeeping after a successful login: resets FailedLogCount and sets LastLoginDate.
 *                Applies only to an active, unlocked user. Does NOT touch ModifiedBy / ModifiedDate (D-05; the
 *                outcome is recorded in LoginHistory). @RowsAffected = 0 means the user is no longer
 *                active / unlocked; the caller rolls back.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Step00_Authentication_Foundation_Spec section 5.2 (N2, D-05, D-06)
 */
CREATE OR ALTER PROCEDURE dbo.SupAppUsrLoginSuccess
    @UserId       INT,
    @RowsAffected INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE dbo.AppUser
    SET FailedLogCount = 0,
        LastLoginDate  = SYSUTCDATETIME()
    WHERE Id = @UserId
      AND IsActive = 1
      AND IsLocked = 0;

    SET @RowsAffected = @@ROWCOUNT;
END
