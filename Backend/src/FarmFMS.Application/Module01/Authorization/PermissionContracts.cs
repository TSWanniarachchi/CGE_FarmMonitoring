using FarmFMS.Application.Module01.Authentication;

/*
 * Description  : Effective permission repository / service contracts (SvaUsrPerm, AD-13).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authorization;

/// <summary>Effective permission data access (<c>dbo.SvaUsrPerm</c>).</summary>
public interface IUserPermissionRepository
{
    Task<IReadOnlyList<EffectivePermissionReadModel>> GetEffectivePermissionsAsync(
        int userId,
        byte platform,
        CancellationToken cancellationToken = default);
}

/// <summary>One row of <c>SvaUsrPerm</c>: a permission code granted on a menu (DISTINCT).</summary>
public sealed class EffectivePermissionReadModel
{
    public string MenuCode { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
}

/// <summary>
/// Scoped, per-request memoized permission lookup (Web platform, D-11). A user without roles has an empty set (D-14).
/// Codes are compared with <see cref="StringComparison.Ordinal"/> (case-sensitive, fail closed): authorization policies
/// are built from the same code constants that are stored in <c>Menu.Code</c> / <c>Permission.Code</c>.
/// </summary>
public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(int userId, string menuCode, string permissionCode, CancellationToken cancellationToken = default);
}
