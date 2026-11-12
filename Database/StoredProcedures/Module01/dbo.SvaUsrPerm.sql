/*
 * Description  : Effective permissions of a user for a platform (read-only): the distinct union over
 *                UserRole -> Role (active) -> RoleMenuAccess -> Menu (active, platform matches) -> Permission (active).
 *                A RoleMenuAccess row means Allow (D-11, C-04). Empty when the user is inactive or locked.
 *                @Platform: 1 = Web, 2 = Mobile; menus with Platform = 3 (Both) always match.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 * Specification: Farm_FMS_Module_01_Database_Specification_v0.2 (Stored Procedures: SvaUsrPerm);
 *                Step00_Authentication_Foundation_Spec section 5.1 (D-11)
 */
CREATE OR ALTER PROCEDURE dbo.SvaUsrPerm
    @UserId   INT,
    @Platform TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SELECT DISTINCT
        m.Id   AS MenuId,
        m.Code AS MenuCode,
        p.Id   AS PermissionId,
        p.Code AS PermissionCode
    FROM dbo.AppUser AS u
    INNER JOIN dbo.UserRole AS ur
        ON ur.UserId = u.Id
    INNER JOIN dbo.Role AS r
        ON r.Id = ur.RoleId AND r.IsActive = 1
    INNER JOIN dbo.RoleMenuAccess AS rma
        ON rma.RoleId = r.Id
    INNER JOIN dbo.Menu AS m
        ON m.Id = rma.MenuId AND m.IsActive = 1 AND (m.Platform = @Platform OR m.Platform = 3)
    INNER JOIN dbo.Permission AS p
        ON p.Id = rma.PermissionId AND p.IsActive = 1
    WHERE u.Id = @UserId
      AND u.IsActive = 1
      AND u.IsLocked = 0;
END
