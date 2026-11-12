/*
 * Description  : Sets the encrypted password payload of a user (EncPassword, PasswordNonce, PasswordTag, KeyVersion,
 *                PwdChangeReq) and the audit columns. Step 0: used only by the Development bootstrap seed
 *                (P-03 Option A needs the AppUser.Id before encryption); no API endpoint calls it.
 *                Errors: 50004 when the user does not exist. @RowsAffected = 1 on success.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Step00_Authentication_Foundation_Spec section 5.2 (N7, D-20; password-set policy P-06 pending)
 */
CREATE OR ALTER PROCEDURE dbo.SupAppUsrPwd
    @UserId        INT,
    @EncPassword   VARBINARY(512),
    @PasswordNonce VARBINARY(12),
    @PasswordTag   VARBINARY(16),
    @KeyVersion    NVARCHAR(50),
    @PwdChangeReq  BIT,
    @ModifiedBy    INT,
    @RowsAffected  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.AppUser WHERE Id = @UserId)
        THROW 50004, N'The AppUser to update does not exist.', 1;

    UPDATE dbo.AppUser
    SET EncPassword   = @EncPassword,
        PasswordNonce = @PasswordNonce,
        PasswordTag   = @PasswordTag,
        KeyVersion    = @KeyVersion,
        PwdChangeReq  = @PwdChangeReq,
        ModifiedBy    = @ModifiedBy,
        ModifiedDate  = SYSUTCDATETIME()
    WHERE Id = @UserId;

    SET @RowsAffected = @@ROWCOUNT;
END
