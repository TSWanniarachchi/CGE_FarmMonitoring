/*
 * Description  : Reads one AppUser profile by Id. Never returns password columns
 *                (EncPassword, PasswordNonce, PasswordTag, KeyVersion). 0 rows = not found.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Farm_FMS_Module_01_Database_Specification_v0.2 (Stored Procedures: SreAppUserById);
 *                Step00_Authentication_Foundation_Spec section 5.1
 */
CREATE OR ALTER PROCEDURE dbo.SreAppUserById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT
        Id,
        Username,
        FirstName,
        LastName,
        Email,
        MobileNo,
        IsActive,
        IsLocked,
        LastLoginDate,
        PwdChangeReq,
        CreatedBy,
        CreatedDate,
        ModifiedBy,
        ModifiedDate
    FROM dbo.AppUser
    WHERE Id = @Id;
END
