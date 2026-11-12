using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Module01.Users;

/*
 * Description  : Resolves the System user Id from the database (AD-14, Step 0 spec D-19) and caches the first positive result.
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Identity;

/// <summary>
/// Process-wide cache of the System user Id (singleton). Only a positive result is stored; "not found" is never cached,
/// so the SYSTEM record created by the bootstrap becomes visible without a restart. Thread-safe.
/// </summary>
internal sealed class SystemUserIdCache
{
    private int _userId;

    public bool TryGet(out int userId)
    {
        userId = Volatile.Read(ref _userId);
        return userId > 0;
    }

    public void Set(int userId)
    {
        if (userId > 0)
            Volatile.Write(ref _userId, userId);
    }
}

/// <summary>
/// Scoped resolver: it uses the scoped repository (and therefore the scoped executor / unit of work), and the singleton
/// <see cref="SystemUserIdCache"/> keeps the result for the life of the process.
/// </summary>
internal sealed class DatabaseSystemUserProvider : ISystemUserProvider
{
    private readonly SystemUserIdCache _cache;
    private readonly IAdministratorBootstrapRepository _repository;
    private readonly ISystemUserSettings _settings;

    public DatabaseSystemUserProvider(
        SystemUserIdCache cache,
        IAdministratorBootstrapRepository repository,
        ISystemUserSettings settings)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async ValueTask<int?> GetSystemUserIdAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGet(out var cached))
            return cached;

        var id = await _repository.GetSystemUserIdAsync(_settings.Username, cancellationToken);
        if (id is not > 0)
            return null;

        _cache.Set(id.Value);
        return id.Value;
    }
}
