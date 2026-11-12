/*
 * Description  : Refresh-token rotation in one transaction. Locks the current session row (UPDLOCK, HOLDLOCK), then
 *                revokes it (RevokedReason 'Rotated', ModifiedBy = @CreatedBy) and inserts the successor, which
 *                inherits UserId, ExpiresDate and LastActDate (D-07: refresh does not extend the idle window);
 *                the successor's CreatedDate = now. Returns @NewId (the new session id).
 *                Errors: 50001 when the current row does not exist, is revoked, absolute-expired, idle, or the
 *                user is inactive / locked / has no active UserAccount / lacks access to @Platform
 *                (1 = Web: CanAccessWeb, 2 = Mobile: CanAccessMobile). A concurrent second use of the same token
 *                sees the row already revoked and gets 50001 (treated as token reuse by the API).
 *                Evaluated with the database clock (UTC).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Change notes : 12-11-2026 (G6 L-5) fail-closed parameter guards before the transaction (THROW 50000):
 *                @IdleTimeoutMinutes > 0, @Platform in (1, 2).
 * Specification: Step00_Authentication_Foundation_Spec section 5.2 (N5, D-07)
 */
CREATE OR ALTER PROCEDURE dbo.ScrUsrSessRotate
    @CurrentSessionId   INT,
    @NewTokenHash       VARBINARY(32),
    @IdleTimeoutMinutes INT,
    @Platform           TINYINT,
    @IPAddress          NVARCHAR(45) = NULL,
    @DeviceInfo         NVARCHAR(500) = NULL,
    @CreatedBy          INT,
    @NewId              INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @IdleTimeoutMinutes IS NULL OR @IdleTimeoutMinutes <= 0
        THROW 50000, N'@IdleTimeoutMinutes must be greater than zero.', 1;
    IF @Platform IS NULL OR @Platform NOT IN (1, 2)
        THROW 50000, N'@Platform must be 1 (Web) or 2 (Mobile).', 1;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @UserId INT,
            @ExpiresDate DATETIME2,
            @LastActDate DATETIME2,
            @RevokedDate DATETIME2;

    BEGIN TRANSACTION;

        SELECT
            @UserId      = s.UserId,
            @ExpiresDate = s.ExpiresDate,
            @LastActDate = s.LastActDate,
            @RevokedDate = s.RevokedDate
        FROM dbo.UserSession AS s WITH (UPDLOCK, HOLDLOCK)
        WHERE s.Id = @CurrentSessionId;

        IF @UserId IS NULL
            THROW 50001, N'The current session does not exist.', 1;

        IF @RevokedDate IS NOT NULL
            THROW 50001, N'The current session is already revoked (rotated, logged out or revoked).', 1;

        IF @ExpiresDate <= @Now
            THROW 50001, N'The current session reached its absolute expiry.', 1;

        IF @LastActDate <= DATEADD(MINUTE, -@IdleTimeoutMinutes, @Now)
            THROW 50001, N'The current session exceeded the idle timeout.', 1;

        IF NOT EXISTS (
            SELECT 1
            FROM dbo.AppUser AS u
            INNER JOIN dbo.UserAccount AS a
                ON a.UserId = u.Id
            WHERE u.Id = @UserId
              AND u.IsActive = 1
              AND u.IsLocked = 0
              AND a.IsActive = 1
              AND ((@Platform = 1 AND a.CanAccessWeb = 1) OR (@Platform = 2 AND a.CanAccessMobile = 1)))
            THROW 50001, N'The user is inactive, locked, has no active account or no access to the platform.', 1;

        UPDATE dbo.UserSession
        SET RevokedDate   = @Now,
            RevokedReason = N'Rotated',
            ModifiedBy    = @CreatedBy,
            ModifiedDate  = @Now
        WHERE Id = @CurrentSessionId;

        INSERT INTO dbo.UserSession
            (UserId, TokenHash, CreatedDate, LastActDate, ExpiresDate, IPAddress, DeviceInfo, CreatedBy)
        VALUES
            (@UserId, @NewTokenHash, @Now, @LastActDate, @ExpiresDate, @IPAddress, @DeviceInfo, @CreatedBy);

        SET @NewId = CAST(SCOPE_IDENTITY() AS INT);

    COMMIT TRANSACTION;
END
