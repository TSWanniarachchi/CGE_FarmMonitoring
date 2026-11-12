/*
 * Description  : Access-token issuing and refresh-token handling abstractions (implemented in Infrastructure, AD-12).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>Issues JWT access tokens (D-23). No permission claims (D-11).</summary>
public interface IAccessTokenIssuer
{
    AccessToken Issue(int userId, int sessionId, string username);
}

/// <summary>An issued access token and its expiry (UTC).</summary>
public sealed record AccessToken(string Token, DateTime ExpiresUtc);

/// <summary>Creates and hashes opaque refresh tokens (D-24).</summary>
public interface ISessionTokenFactory
{
    /// <summary>A new token: 32 CSPRNG bytes as base64url (43 characters) and the SHA-256 hash of the raw bytes.</summary>
    SessionToken Create();

    /// <summary>
    /// Hashes a presented token. Returns false (no exception) for anything that does not decode to exactly 32 bytes.
    /// </summary>
    bool TryHash(string? rawToken, out byte[] hash);
}

/// <summary>A refresh token for the client (<see cref="RawToken"/>, shown once) and its database hash.</summary>
public sealed record SessionToken(string RawToken, byte[] Hash);
