using FarmFMS.API.Common.Errors;
using FarmFMS.API.Common.Security;
using FarmFMS.Application.Abstractions.Identity;
using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Application.Module01.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.RateLimiting;

/*
 * Description  : Authentication endpoints: login, refresh, logout and the current user (Step 0 spec 8.1, D-29).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.API.Controllers.Module01;

/// <summary>
/// Sets <c>Cache-Control: no-store</c> and <c>Pragma: no-cache</c> on every response of the action (tokens and the
/// profile must never be cached).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
internal sealed class NoStoreAttribute : ResultFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        context.HttpContext.Response.Headers.Pragma = "no-cache";
    }
}

/// <summary>
/// Sign in, refresh, sign out and read the signed-in user. Login, refresh and logout are the only anonymous
/// endpoints (AD-13 allowlist); every other endpoint requires authentication by default.
/// </summary>
[Route("api/auth")]
[Produces("application/json")]
[NoStore]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthenticationService _authentication;
    private readonly ICurrentUserProfileService _profile;
    private readonly ICurrentUserService _currentUser;

    public AuthController(
        IAuthenticationService authentication,
        ICurrentUserProfileService profile,
        ICurrentUserService currentUser)
    {
        _authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    /// <summary>Signs in with username and password.</summary>
    /// <remarks>
    /// Returns a short-lived access token (JWT) and a refresh token. Every failure except missing web access is the
    /// same generic 401. Rate limited per client IP.
    /// </remarks>
    /// <response code="200">Signed in.</response>
    /// <response code="400">The request is malformed.</response>
    /// <response code="401">Invalid username or password.</response>
    /// <response code="403">The account has no access to this application.</response>
    /// <response code="429">Too many requests.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingOptions.LoginPolicy)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        OkOrProblem(await _authentication.LoginAsync(request, ClientContextFactory.From(HttpContext), cancellationToken));

    /// <summary>Exchanges a refresh token for new tokens (rotation).</summary>
    /// <remarks>Presenting an already used refresh token revokes every session of the user.</remarks>
    /// <response code="200">New tokens.</response>
    /// <response code="400">The request is malformed.</response>
    /// <response code="401">The refresh token is not valid.</response>
    /// <response code="429">Too many requests.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingOptions.RefreshPolicy)]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken) =>
        OkOrProblem(await _authentication.RefreshAsync(request, ClientContextFactory.From(HttpContext), cancellationToken));

    /// <summary>Ends the session of a refresh token.</summary>
    /// <remarks>Always 204 for a well-formed token, whether or not it is known (no oracle).</remarks>
    /// <response code="204">Signed out.</response>
    /// <response code="400">The request is malformed.</response>
    /// <response code="429">Too many requests.</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingOptions.RefreshPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken) =>
        NoContentOrProblem(await _authentication.LogoutAsync(request, ClientContextFactory.From(HttpContext), cancellationToken));

    /// <summary>The signed-in user and the effective permissions.</summary>
    /// <response code="200">The user (no password fields) and permissions.</response>
    /// <response code="401">Not signed in, or the session is no longer valid.</response>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not { } userId)
            return ProblemFrom(AuthErrors.SessionInvalid);

        return OkOrProblem(await _profile.GetAsync(userId, cancellationToken));
    }
}
