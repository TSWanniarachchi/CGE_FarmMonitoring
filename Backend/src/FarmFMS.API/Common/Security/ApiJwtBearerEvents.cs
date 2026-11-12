using System.Globalization;
using System.Security.Claims;
using FarmFMS.Application.Common.Results;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

/*
 * Description  : JwtBearer events: per-request session validation and generic 401 / 403 responses (Step 0 spec 3.5, 8.2).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.API.Common.Security;

/// <summary>
/// After the token signature, issuer, audience and lifetime are validated, every request checks the session
/// (<see cref="ISessionValidationService"/>): revocation, absolute expiry, the 30-minute idle timeout, user state and
/// platform access (D-08). Any failure (including an unresolvable System user, which fails closed) fails
/// authentication, so the response is 401, never 200. Responses never reveal why.
/// </summary>
internal sealed class ApiJwtBearerEvents : JwtBearerEvents
{
    public const string UnauthenticatedMessage = "Authentication is required. Please sign in.";

    /// <summary>
    /// M-1 (AC-10, D-08): an endpoint marked <c>[AllowAnonymous]</c> (login, refresh, logout, health) never authenticates
    /// the bearer token, so it never validates or touches the session. Otherwise a client that only refreshes could keep
    /// an idle session alive (<c>SvaUsrSess</c> advances <c>LastActDate</c>, and rotation copies it).
    /// Routing runs before authentication (implicit <c>UseRouting</c> of the minimal host), so the endpoint is known here.
    /// </summary>
    public override Task MessageReceived(MessageReceivedContext context)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            context.NoResult();

        return Task.CompletedTask;
    }

    /// <remarks>
    /// L-4: a transient database failure while validating the session (<c>DataAccessException</c>) is not caught here on
    /// purpose. It propagates through the JwtBearer handler to the global exception handler, which answers 503
    /// ProblemDetails (generic), never 401 and never 200. Covered by a host test.
    /// </remarks>
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger<ApiJwtBearerEvents>();

        if (!TryReadId(context.Principal, JwtValidation.SubjectClaim, out var userId) ||
            !TryReadId(context.Principal, JwtValidation.SessionIdClaim, out var sessionId))
        {
            logger.LogWarning("AuthenticationRejected: token without valid subject / session claims");
            context.Fail("Invalid token.");
            return;
        }

        var validation = context.HttpContext.RequestServices.GetRequiredService<ISessionValidationService>();
        var result = await validation.ValidateAsync(sessionId, userId, context.HttpContext.RequestAborted);

        if (result.IsSuccess)
            return;

        if (result.Error.Type == ErrorType.Failure)
            logger.LogError("Session validation failed closed ({ErrorCode}); the request is rejected", result.Error.Code);

        context.Fail("Invalid session.");
    }

    public override Task Challenge(JwtBearerChallengeContext context)
    {
        // Own the response: no error / error_description in WWW-Authenticate, no exception text in the body.
        context.HandleResponse();
        context.Response.Headers.WWWAuthenticate = "Bearer";

        var rejected = context.AuthenticateFailure is not null;
        return ProblemResponses.WriteAsync(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            rejected ? AuthErrors.SessionInvalid.Message : UnauthenticatedMessage,
            rejected ? ProblemResponses.SessionInvalid : ProblemResponses.Unauthenticated);
    }

    public override Task Forbidden(ForbiddenContext context) =>
        ProblemResponses.WriteAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "You do not have permission to perform this action.",
            ProblemResponses.Forbidden);

    private static bool TryReadId(ClaimsPrincipal? principal, string claimType, out int id)
    {
        id = 0;
        var value = principal?.FindFirstValue(claimType);

        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;
    }
}
