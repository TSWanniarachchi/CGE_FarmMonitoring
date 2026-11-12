/*
 * Description  : Creates an AppUser. Step 0: used only by the Development bootstrap seed (D-20); the full rules of
 *                the Admin create-user feature belong to a later step.
 *                IsActive defaults to 1 (table default). The optional @IsActive (default 1) exists only so the
 *                seed can create the SYSTEM user inactive (D-19); it is an addition to the specification signature.
 *                Errors: 50005 username already in use (case-insensitive by collation, D-21). The unique
 *                constraint UQAppUser_Username is the final guard against a race (SQL error 2627 -> 409).
 *                Returns @NewId.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Farm_FMS_Module_01_Database_Specification_v0.2 (Stored Procedures: ScrAppUser);
 *                Step00_Authentication_Foundation_Spec section 5.3 (D-19, D-20)
 */
CREATE OR ALTER PROCEDURE dbo.ScrAppUser
    @Username      NVARCHAR(100),
    @FirstName     NVARCHAR(100),
    @LastName      NVARCHAR(100) = NULL,
    @Email         NVARCHAR(255) = NULL,
    @MobileNo      NVARCHAR(20) = NULL,
    @EncPassword   VARBINARY(512),
    @PasswordNonce VARBINARY(12),
    @PasswordTag   VARBINARY(16),
    @KeyVersion    NVARCHAR(50),
    @PwdChangeReq  BIT,
    @IsActive      BIT = 1,
    @CreatedBy     INT,
    @NewId         INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM dbo.AppUser WITH (UPDLOCK, HOLDLOCK) WHERE Username = @Username)
            THROW 50005, N'The username is already in use.', 1;

        INSERT INTO dbo.AppUser
            (Username, EncPassword, PasswordNonce, PasswordTag, KeyVersion, FirstName, LastName, Email, MobileNo,
             IsActive, PwdChangeReq, CreatedBy, CreatedDate)
        VALUES
            (@Username, @EncPassword, @PasswordNonce, @PasswordTag, @KeyVersion, @FirstName, @LastName, @Email, @MobileNo,
             @IsActive, @PwdChangeReq, @CreatedBy, SYSUTCDATETIME());

        SET @NewId = CAST(SCOPE_IDENTITY() AS INT);

    COMMIT TRANSACTION;
END
