/*
 * Description  : Current user response for GET api/auth/me (Step 0 spec section 6.4).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>The signed-in user with effective permissions. Never contains password fields.</summary>
public sealed class CurrentUserResponse
{
    public int Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public bool PasswordChangeRequired { get; init; }

    public IReadOnlyList<PermissionDto> Permissions { get; init; } = [];
}

/// <summary>One effective permission: the permission code granted on a menu (spec SvaUsrPerm).</summary>
public sealed class PermissionDto
{
    public string MenuCode { get; init; } = string.Empty;

    public string PermissionCode { get; init; } = string.Empty;
}
