using FarmFMS.Application.Module01.Authentication;

/*
 * Description  : Scoped, memoized effective-permission lookup (SvaUsrPerm) (Step 0 spec sections 3.5, 6.2, D-11).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authorization;

/// <summary>
/// Registered scoped, so the lookup runs at most once per user per request. Failed lookups (exceptions) are not memoized.
/// Not thread-safe by design: one request scope is processed sequentially.
/// </summary>
internal sealed class PermissionService : IPermissionService
{
    private readonly IUserPermissionRepository _repository;
    private readonly Dictionary<int, IReadOnlyList<PermissionDto>> _cache = [];

    public PermissionService(IUserPermissionRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return [];

        if (_cache.TryGetValue(userId, out var cached))
            return cached;

        var rows = await _repository.GetEffectivePermissionsAsync(userId, SessionPolicy.PlatformWeb, cancellationToken);

        IReadOnlyList<PermissionDto> permissions = rows
            .Select(row => new PermissionDto { MenuCode = row.MenuCode, PermissionCode = row.PermissionCode })
            .ToList();

        _cache[userId] = permissions;
        return permissions;
    }

    public async Task<bool> HasPermissionAsync(
        int userId,
        string menuCode,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(menuCode) || string.IsNullOrEmpty(permissionCode))
            return false;

        var permissions = await GetPermissionsAsync(userId, cancellationToken);

        return permissions.Any(p =>
            string.Equals(p.MenuCode, menuCode, StringComparison.Ordinal) &&
            string.Equals(p.PermissionCode, permissionCode, StringComparison.Ordinal));
    }
}
