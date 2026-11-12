using FarmFMS.Application.Common.Results;

/*
 * Description  : Authentication use cases: login, refresh, logout (Step 0 spec sections 3.2 - 3.4).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

public interface IAuthenticationService
{
    /// <summary>
    /// Failures: <see cref="AuthErrors.InvalidCredentials"/> (401, generic for every reason except platform access),
    /// <see cref="AuthErrors.PlatformAccessDenied"/> (403), or a generic Failure when the System user cannot be resolved.
    /// </summary>
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request, ClientContext client, CancellationToken cancellationToken = default);

    /// <summary>Failures: <see cref="AuthErrors.InvalidRefreshToken"/> (401) or a generic Failure (System user missing).</summary>
    Task<Result<TokenResponse>> RefreshAsync(RefreshRequest request, ClientContext client, CancellationToken cancellationToken = default);

    /// <summary>Succeeds for any well-formed token, known or not (no oracle).</summary>
    Task<Result> LogoutAsync(LogoutRequest request, ClientContext client, CancellationToken cancellationToken = default);
}

/// <summary>Per-request session validation (N6) used after JWT validation (spec section 3.5).</summary>
public interface ISessionValidationService
{
    /// <summary>Success for a valid session; <see cref="AuthErrors.SessionInvalid"/> otherwise.</summary>
    Task<Result> ValidateAsync(int sessionId, int userId, CancellationToken cancellationToken = default);
}
