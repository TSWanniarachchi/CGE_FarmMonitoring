/*
 * Description  : Platform a menu applies to, stored in Menu.Platform (TINYINT).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Domain.Module01.Authorization;

/// <summary>Platform a menu applies to, stored in Menu.Platform (TINYINT).</summary>
public enum MenuPlatform : byte
{
    Web = 1,
    Mobile = 2,
    Both = 3,
}
