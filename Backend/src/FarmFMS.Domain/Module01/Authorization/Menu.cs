/*
 * Description  : Menu item (table Menu); ParentMenuId is a plain column, not a foreign key.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authorization;

/// <summary>Menu item (table Menu); ParentMenuId is a plain column, not a foreign key.</summary>
public class Menu : FarmFMS.Domain.Common.AuditableEntity
{
    public int? ParentMenuId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public string? Route { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>TINYINT; values of <see cref="MenuPlatform"/>.</summary>
    public byte Platform { get; set; }

    public bool IsActive { get; set; }
}
