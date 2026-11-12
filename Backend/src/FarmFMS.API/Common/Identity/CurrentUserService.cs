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
/// Access tokens are validated with <c>MapInboundClaims = false</c>, so the claims keep their JWT names:
/// the user ID is the "sub" claim, the session ID the "sid" claim and the name the "name" claim
/// (Step 0 spec 3.1, 8.2). <see cref="ClaimTypes.NameIdentifier"/> / <see cref="ClaimTypes.Name"/> are accepted as
/// fallbacks for principals built without the JWT handler.
/// There is deliberately NO fallback "system" user: the system-actor convention for CreatedBy is not yet approved.
/// </para>
/// </summary>
internal sealed class CurrentUserService : ICurrentUserService
{
    private const string SubjectClaimType = "sub";
    private const string SessionIdClaimType = "sid";
    private const string NameClaimType = "name";

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

    public int? SessionId
    {
        get
        {
            if (!IsAuthenticated) return null;

            var value = User!.FindFirstValue(SessionIdClaimType);

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

            var name = User!.FindFirstValue(NameClaimType) ?? User!.Identity!.Name;
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }
}
