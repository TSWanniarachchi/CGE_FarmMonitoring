/*
 * Description  : Role (table Role).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authorization;

/// <summary>Role (table Role).</summary>
public class Role : FarmFMS.Domain.Common.AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public bool IsActive { get; set; }
}
