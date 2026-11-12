/*
 * Description  : User to role assignment (table UserRole).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authorization;

/// <summary>User to role assignment (table UserRole).</summary>
public class UserRole : FarmFMS.Domain.Common.AuditableEntity
{
    public int UserId { get; set; }

    public int RoleId { get; set; }
}
