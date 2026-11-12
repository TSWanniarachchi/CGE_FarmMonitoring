/*
 * Description  : Creates a session (one refresh-token row) at login. Stores only the SHA-256 token hash.
 *                ExpiresDate = now + @AbsoluteLifetimeMinutes (absolute end, D-10); LastActDate = CreatedDate = now.
 *                Returns @NewId (= the session id, "sid") and @ExpiresDate (OUTPUT).
 *                Errors: 50002 user missing, inactive or locked. A duplicate token hash raises SQL error 2627 (409).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Step00_Authentication_Foundation_Spec section 5.2 (N1, D-04, D-09, D-10);
 *                gap DBG-012 (no specification procedure creates a UserSession row)
 */
CREATE OR ALTER PROCEDURE dbo.ScrUsrSess
    @UserId                  INT,
    @TokenHash               VARBINARY(32),
    @AbsoluteLifetimeMinutes INT,
    @IPAddress               NVARCHAR(45) = NULL,
    @DeviceInfo              NVARCHAR(500) = NULL,
    @CreatedBy               INT,
    @ExpiresDate             DATETIME2 OUTPUT,
    @NewId                   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @AbsoluteLifetimeMinutes IS NULL OR @AbsoluteLifetimeMinutes <= 0
        THROW 50000, N'@AbsoluteLifetimeMinutes must be greater than zero.', 1;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @Expires DATETIME2 = DATEADD(MINUTE, @AbsoluteLifetimeMinutes, @Now);

    BEGIN TRANSACTION;

        -- Logical reference and state check (no physical foreign keys, AD-07); the shared lock is held until commit
        -- so the user cannot be deactivated or locked between the check and the insert.
        IF NOT EXISTS (SELECT 1 FROM dbo.AppUser WITH (HOLDLOCK) WHERE Id = @UserId AND IsActive = 1 AND IsLocked = 0)
            THROW 50002, N'The user does not exist, is inactive or is locked; no session was created.', 1;

        INSERT INTO dbo.UserSession
            (UserId, TokenHash, CreatedDate, LastActDate, ExpiresDate, IPAddress, DeviceInfo, CreatedBy)
        VALUES
            (@UserId, @TokenHash, @Now, @Now, @Expires, @IPAddress, @DeviceInfo, @CreatedBy);

        SET @NewId = CAST(SCOPE_IDENTITY() AS INT);
        SET @ExpiresDate = @Expires;

    COMMIT TRANSACTION;
END
