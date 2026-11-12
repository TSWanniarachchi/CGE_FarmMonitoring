/*
 * Description  : Counts one failed login. A single atomic UPDATE increments FailedLogCount and, when the new count
 *                reaches @MaxFailedAttempts, sets IsLocked = 1 and LockedDate = now, so concurrent failures are
 *                counted exactly. Applies only to an active, unlocked user (attempts on a locked account are not
 *                counted, D-06). Does NOT touch ModifiedBy / ModifiedDate (D-05).
 *                @RowsAffected = 0: user missing, inactive or already locked (@IsLocked stays 0).
 *                @IsLocked OUTPUT = 1 when this call locked the account.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Step00_Authentication_Foundation_Spec section 5.2 (N3, D-05, D-06)
 */
CREATE OR ALTER PROCEDURE dbo.SupAppUsrLoginFailure
    @UserId            INT,
    @MaxFailedAttempts INT,
    @IsLocked          BIT OUTPUT,
    @RowsAffected      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @MaxFailedAttempts IS NULL OR @MaxFailedAttempts < 1
        THROW 50000, N'@MaxFailedAttempts must be at least 1.', 1;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    SET @IsLocked = 0;

    -- All right-hand sides read the pre-update row values.
    UPDATE dbo.AppUser
    SET FailedLogCount = FailedLogCount + 1,
        IsLocked       = CASE WHEN FailedLogCount + 1 >= @MaxFailedAttempts THEN 1 ELSE 0 END,
        LockedDate     = CASE WHEN FailedLogCount + 1 >= @MaxFailedAttempts THEN @Now ELSE LockedDate END,
        @IsLocked      = CASE WHEN FailedLogCount + 1 >= @MaxFailedAttempts THEN 1 ELSE 0 END
    WHERE Id = @UserId
      AND IsActive = 1
      AND IsLocked = 0;

    SET @RowsAffected = @@ROWCOUNT;
END
