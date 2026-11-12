/*
 * Description  : Reads a session by its refresh-token hash together with the user and account state (read-only).
 *                IsExpired = ExpiresDate <= now; IsIdle = LastActDate <= now - @IdleTimeoutMinutes (database clock, UTC).
 *                0 rows = unknown token hash (or the owning AppUser row is missing).
 *                AccountIsActive, CanAccessWeb and CanAccessMobile are NULL when the user has no UserAccount.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Change notes : 12-11-2026 (G6 L-5) fail-closed guard (THROW 50000): @IdleTimeoutMinutes > 0.
 * Specification: Step00_Authentication_Foundation_Spec section 5.2 (N4, D-07)
 */
CREATE OR ALTER PROCEDURE dbo.SreUsrSessByTokenHash
    @TokenHash          VARBINARY(32),
    @IdleTimeoutMinutes INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @IdleTimeoutMinutes IS NULL OR @IdleTimeoutMinutes <= 0
        THROW 50000, N'@IdleTimeoutMinutes must be greater than zero.', 1;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @IdleLimit DATETIME2 = DATEADD(MINUTE, -@IdleTimeoutMinutes, @Now);

    SELECT
        s.Id            AS SessionId,
        s.UserId,
        s.CreatedDate,
        s.LastActDate,
        s.ExpiresDate,
        s.RevokedDate,
        s.RevokedReason,
        u.Username,
        u.FirstName,
        u.LastName,
        u.IsActive,
        u.IsLocked,
        u.PwdChangeReq,
        a.IsActive      AS AccountIsActive,
        a.CanAccessWeb,
        a.CanAccessMobile,
        CAST(CASE WHEN s.ExpiresDate <= @Now THEN 1 ELSE 0 END AS BIT)      AS IsExpired,
        CAST(CASE WHEN s.LastActDate <= @IdleLimit THEN 1 ELSE 0 END AS BIT) AS IsIdle
    FROM dbo.UserSession AS s
    INNER JOIN dbo.AppUser AS u
        ON u.Id = s.UserId
    LEFT JOIN dbo.UserAccount AS a
        ON a.UserId = u.Id
    WHERE s.TokenHash = @TokenHash;
END
