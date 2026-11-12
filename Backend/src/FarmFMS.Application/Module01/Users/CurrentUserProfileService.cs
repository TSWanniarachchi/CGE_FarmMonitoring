using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Authorization;

/*
 * Description  : Current-user profile for GET api/auth/me (Step 0 spec sections 6.2, 6.4).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Users;

internal sealed class CurrentUserProfileService : ICurrentUserProfileService
{
    private readonly IAppUserReadRepository _users;
    private readonly IPermissionService _permissions;

    public CurrentUserProfileService(IAppUserReadRepository users, IPermissionService permissions)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    public async Task<Result<CurrentUserResponse>> GetAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return AuthErrors.SessionInvalid;

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive || user.IsLocked)
            return AuthErrors.SessionInvalid;

        var permissions = await _permissions.GetPermissionsAsync(userId, cancellationToken);

        return new CurrentUserResponse
        {
            Id = user.Id,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PasswordChangeRequired = user.PwdChangeReq,
            Permissions = permissions
        };
    }
}
