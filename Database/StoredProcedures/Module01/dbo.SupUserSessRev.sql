/*
 * Description  : Revokes one session of a user (@SessionId) or all non-revoked sessions of the user
 *                (@SessionId NULL). Only rows with RevokedDate IS NULL are changed.
 *                @RowsAffected = 0 means nothing to revoke (not an error).
 *                Note: the specification defines no index on UserSession.UserId (C-08); all-sessions mode scans.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Farm_FMS_Module_01_Database_Specification_v0.2 (Stored Procedures: SupUserSessRev);
 *                Step00_Authentication_Foundation_Spec section 5.1 (D-07)
 */
CREATE OR ALTER PROCEDURE dbo.SupUserSessRev
    @UserId         INT,
    @SessionId      INT = NULL,
    @RevokedReason  NVARCHAR(250),
    @ModifiedBy     INT,
    @RowsAffected   INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    UPDATE dbo.UserSession
    SET RevokedDate   = @Now,
        RevokedReason = @RevokedReason,
        ModifiedBy    = @ModifiedBy,
        ModifiedDate  = @Now
    WHERE UserId = @UserId
      AND RevokedDate IS NULL
      AND (@SessionId IS NULL OR Id = @SessionId);

    SET @RowsAffected = @@ROWCOUNT;
END
