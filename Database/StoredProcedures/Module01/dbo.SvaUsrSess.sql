/*
 * Description  : Per-request session validation (D-08). Returns one row: SessionStatus TINYINT
 *                0 Valid, 1 NotFound / UserMismatch, 2 Revoked, 3 Expired, 4 Idle, 5 UserInactive, 6 UserLocked,
 *                7 PlatformDenied (1 = Web: CanAccessWeb, 2 = Mobile: CanAccessMobile; no active UserAccount = denied).
 *                Evaluation order: 1, 2, 3, 4, 5, 6, 7 (first match wins), with the database clock (UTC).
 *                Writes (a status row, never THROW, so the revocation is committed):
 *                  - 3 Expired -> session revoked, RevokedReason 'Expired';
 *                  - 4 Idle    -> session revoked, RevokedReason 'IdleTimeout';
 *                  - 0 Valid   -> LastActDate = now, only when LastActDate <= now - @TouchIntervalSeconds.
 *                Statuses 5, 6 and 7 do NOT revoke the session (status only, as specified); the refresh path
 *                revokes with 'AccountState'. ModifiedBy / ModifiedDate = @ModifiedBy / now are set on the Expired and
 *                IdleTimeout revocations only; the throttled activity touch updates LastActDate ONLY (activity
 *                bookkeeping, like N2/N3; the N6 actor is the System user and must not be stamped on every request).
 *                Each write is one statement.
 *                The 'Sva' prefix is kept although the procedure writes (validate-and-touch in one round trip).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Change notes : 12-11-2026 (G6 L-5) fail-closed parameter guards (THROW 50000): @IdleTimeoutMinutes > 0,
 *                @TouchIntervalSeconds >= 0, @Platform in (1, 2).
 * Specification: Step00_Authentication_Foundation_Spec section 5.2 (N6, D-08)
 */
CREATE OR ALTER PROCEDURE dbo.SvaUsrSess
    @SessionId           INT,
    @UserId              INT,
    @IdleTimeoutMinutes  INT,
    @TouchIntervalSeconds INT,
    @Platform            TINYINT,
    @ModifiedBy          INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @IdleTimeoutMinutes IS NULL OR @IdleTimeoutMinutes <= 0
        THROW 50000, N'@IdleTimeoutMinutes must be greater than zero.', 1;
    IF @TouchIntervalSeconds IS NULL OR @TouchIntervalSeconds < 0
        THROW 50000, N'@TouchIntervalSeconds must not be negative.', 1;
    IF @Platform IS NULL OR @Platform NOT IN (1, 2)
        THROW 50000, N'@Platform must be 1 (Web) or 2 (Mobile).', 1;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @Status TINYINT = 0;

    DECLARE @SessionUserId INT,
            @LastActDate DATETIME2,
            @ExpiresDate DATETIME2,
            @RevokedDate DATETIME2;

    SELECT
        @SessionUserId = s.UserId,
        @LastActDate   = s.LastActDate,
        @ExpiresDate   = s.ExpiresDate,
        @RevokedDate   = s.RevokedDate
    FROM dbo.UserSession AS s
    WHERE s.Id = @SessionId;

    IF @SessionUserId IS NULL OR @SessionUserId <> @UserId
    BEGIN
        SET @Status = 1;
    END
    ELSE IF @RevokedDate IS NOT NULL
    BEGIN
        SET @Status = 2;
    END
    ELSE IF @ExpiresDate <= @Now
    BEGIN
        SET @Status = 3;

        UPDATE dbo.UserSession
        SET RevokedDate = @Now, RevokedReason = N'Expired', ModifiedBy = @ModifiedBy, ModifiedDate = @Now
        WHERE Id = @SessionId AND RevokedDate IS NULL;
    END
    ELSE IF @LastActDate <= DATEADD(MINUTE, -@IdleTimeoutMinutes, @Now)
    BEGIN
        SET @Status = 4;

        UPDATE dbo.UserSession
        SET RevokedDate = @Now, RevokedReason = N'IdleTimeout', ModifiedBy = @ModifiedBy, ModifiedDate = @Now
        WHERE Id = @SessionId AND RevokedDate IS NULL;
    END
    ELSE
    BEGIN
        DECLARE @UserIsActive BIT,
                @UserIsLocked BIT,
                @AccountIsActive BIT,
                @HasPlatformAccess BIT;

        SELECT
            @UserIsActive      = u.IsActive,
            @UserIsLocked      = u.IsLocked,
            @AccountIsActive   = a.IsActive,
            @HasPlatformAccess = CASE WHEN @Platform = 1 THEN a.CanAccessWeb
                                      WHEN @Platform = 2 THEN a.CanAccessMobile
                                      ELSE 0 END
        FROM dbo.AppUser AS u
        LEFT JOIN dbo.UserAccount AS a
            ON a.UserId = u.Id
        WHERE u.Id = @UserId;

        IF @UserIsActive IS NULL OR @UserIsActive = 0
            SET @Status = 5;
        ELSE IF @UserIsLocked = 1
            SET @Status = 6;
        ELSE IF ISNULL(@AccountIsActive, 0) = 0 OR ISNULL(@HasPlatformAccess, 0) = 0
            SET @Status = 7;
        ELSE IF @LastActDate <= DATEADD(SECOND, -@TouchIntervalSeconds, @Now)
        BEGIN
            -- Throttled activity touch (idle precision = touch interval).
            UPDATE dbo.UserSession
            SET LastActDate = @Now
            WHERE Id = @SessionId AND RevokedDate IS NULL;
        END
    END

    SELECT @Status AS SessionStatus;
END
