using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;

/*
 * Description  : User read repository and current-user profile service contracts (SreAppUserById).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Users;

/// <summary>User read access (<c>dbo.SreAppUserById</c>). Never returns password columns.</summary>
public interface IAppUserReadRepository
{
    Task<AppUserProfileReadModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>The <c>SreAppUserById</c> row (no password columns).</summary>
public sealed class AppUserProfileReadModel
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? MobileNo { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public bool PwdChangeReq { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

/// <summary>The profile and effective permissions of the signed-in user (<c>GET api/auth/me</c>).</summary>
public interface ICurrentUserProfileService
{
    /// <summary>Unauthorized when the user no longer exists or is inactive.</summary>
    Task<Result<CurrentUserResponse>> GetAsync(int userId, CancellationToken cancellationToken = default);
}
