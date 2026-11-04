using System.Globalization;
using System.Security.Claims;
using FarmFMS.Application.Abstractions.Identity;

/*
 * Description  : Resolves the current request's user from the HTTP context.
 * Author       : T.S.Wanniarachchi
 * Created Date : 04-11-2026
 */
namespace FarmFMS.API.Common.Identity;

/// <summary>
/// Reads the authenticated user from <c>HttpContext.User</c>.
/// <para>
/// The user ID is read from the "sub" claim (or <see cref="ClaimTypes.NameIdentifier"/>, which the JWT handler
/// maps "sub" to by default). The final claim set is defined with authentication (Step 04).
/// There is deliberately NO fallback "system" user: the system-actor convention for CreatedBy is not yet approved.
/// </para>
/// </summary>
internal sealed class CurrentUserService : ICurrentUserService
{
    private const string SubjectClaimType = "sub";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ??
            throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public int? UserId
    {
        get
        {
            if (!IsAuthenticated) return null;

            var value = User!.FindFirstValue(SubjectClaimType) ?? User!.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0
                ? id
                : null;
        }
    }

    public string? UserName
    {
        get
        {
            if (!IsAuthenticated) return null;

            var name = User!.Identity!.Name;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }
}
