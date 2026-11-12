/*
 * Description  : Role access to a menu with a permission (table RoleMenuAccess).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authorization;

/// <summary>Role access to a menu with a permission (table RoleMenuAccess).</summary>
public class RoleMenuAccess : FarmFMS.Domain.Common.AuditableEntity
{
    public int RoleId { get; set; }

    public int MenuId { get; set; }

    public int PermissionId { get; set; }
}
