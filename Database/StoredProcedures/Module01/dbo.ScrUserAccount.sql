/*
 * Description  : Creates the UserAccount (Process link and platform access) of a user. One account per user.
 *                Step 0: used only by the Development bootstrap seed (D-20).
 *                Logical reference checks (no physical foreign keys, AD-07): the AppUser must exist (50004).
 *                LIMITATION (DBG-002): @ProcessId cannot be validated against the Process master because that master
 *                is owned by another module and is undefined; only @ProcessId > 0 is checked here (otherwise the
 *                generic error 50000 is raised, as it is a caller programming error). Full ProcessId validation
 *                belongs to the later user-management step. IsActive defaults to 1 (table default).
 *                Errors: 50004 user not found; 50006 the user already has an account. Returns @NewId.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Farm_FMS_Module_01_Database_Specification_v0.2 (Stored Procedures: ScrUserAccount);
 *                Step00_Authentication_Foundation_Spec section 5.3 (D-20, C-09)
 */
CREATE OR ALTER PROCEDURE dbo.ScrUserAccount
    @UserId          INT,
    @ProcessId       INT,
    @CanAccessWeb    BIT,
    @CanAccessMobile BIT,
    @CreatedBy       INT,
    @NewId           INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @ProcessId IS NULL OR @ProcessId <= 0
        THROW 50000, N'@ProcessId must be a positive Process Id (the Process master cannot be checked, DBG-002).', 1;

    BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.AppUser WITH (HOLDLOCK) WHERE Id = @UserId)
            THROW 50004, N'The AppUser does not exist.', 1;

        IF EXISTS (SELECT 1 FROM dbo.UserAccount WITH (UPDLOCK, HOLDLOCK) WHERE UserId = @UserId)
            THROW 50006, N'The user already has an account.', 1;

        INSERT INTO dbo.UserAccount
            (UserId, ProcessId, CanAccessWeb, CanAccessMobile, CreatedBy, CreatedDate)
        VALUES
            (@UserId, @ProcessId, @CanAccessWeb, @CanAccessMobile, @CreatedBy, SYSUTCDATETIME());

        SET @NewId = CAST(SCOPE_IDENTITY() AS INT);

    COMMIT TRANSACTION;
END
