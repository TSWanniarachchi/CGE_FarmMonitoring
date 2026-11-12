/*
 * Description  : Appends a login history event (append-only; no update or delete procedure exists).
 *                EventType: 1 = Success, 2 = Failed, 3 = Logout (D-16). EventDate = CreatedDate = now (UTC).
 *                @UserId is NULL for an unknown username.
 *                Errors: 50003 EventType not in (1, 2, 3); 50004 @UserId given but no AppUser row.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Farm_FMS_Module_01_Database_Specification_v0.2 (Stored Procedures: ScrLoginHist);
 *                Step00_Authentication_Foundation_Spec section 5.1 (D-16, D-17, D-18)
 */
CREATE OR ALTER PROCEDURE dbo.ScrLoginHist
    @UserId        INT = NULL,
    @Username      NVARCHAR(100),
    @EventType     TINYINT,
    @IPAddress     NVARCHAR(45) = NULL,
    @DeviceInfo    NVARCHAR(500) = NULL,
    @FailureReason NVARCHAR(250) = NULL,
    @CreatedBy     INT,
    @NewId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @EventType NOT IN (1, 2, 3)
        THROW 50003, N'LoginHistory EventType must be 1 (Success), 2 (Failed) or 3 (Logout).', 1;

    -- Logical reference check (no physical foreign keys, AD-07).
    IF @UserId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM dbo.AppUser WHERE Id = @UserId)
        THROW 50004, N'The AppUser referenced by @UserId does not exist.', 1;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    INSERT INTO dbo.LoginHistory
        (UserId, Username, EventType, EventDate, IPAddress, DeviceInfo, FailureReason, CreatedBy, CreatedDate)
    VALUES
        (@UserId, @Username, @EventType, @Now, @IPAddress, @DeviceInfo, @FailureReason, @CreatedBy, @Now);

    SET @NewId = CAST(SCOPE_IDENTITY() AS INT);
END
