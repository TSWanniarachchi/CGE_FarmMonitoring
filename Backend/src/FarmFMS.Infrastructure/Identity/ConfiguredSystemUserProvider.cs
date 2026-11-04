using FarmFMS.Application.Abstractions.Identity;
using Microsoft.Extensions.Options;

/*
 * Description  : Provides the System user ID from validated configuration.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.Infrastructure.Identity;

/// <summary>
/// Reads the System user ID from <see cref="SystemUserOptions"/>.
/// A Stored Procedure lookup can replace this once the Module 01 specification defines one
/// (see Architecture Decision AD-14).
/// </summary>
internal sealed class ConfiguredSystemUserProvider : ISystemUserProvider
{
    private readonly int? _userId;

    public ConfiguredSystemUserProvider(IOptions<SystemUserOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _userId = options.Value.UserId;
    }

    public ValueTask<int?> GetSystemUserIdAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(_userId);
}
