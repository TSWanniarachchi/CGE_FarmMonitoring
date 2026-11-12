using System.Buffers.Text;
using System.Security.Cryptography;
using FarmFMS.Application.Module01.Authentication;

/*
 * Description  : Creates and hashes opaque refresh tokens (Step 0 spec section 7.2, D-24).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Security;

/// <summary>
/// Token = 32 CSPRNG bytes encoded as base64url (43 characters); the database stores only SHA-256 of the raw 32 bytes.
/// </summary>
internal sealed class SessionTokenFactory : ISessionTokenFactory
{
    private const int TokenBytes = 32;

    public SessionToken Create()
    {
        var raw = RandomNumberGenerator.GetBytes(TokenBytes);

        try
        {
            return new SessionToken(Base64Url.EncodeToString(raw), SHA256.HashData(raw));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    public bool TryHash(string? rawToken, out byte[] hash)
    {
        hash = [];

        if (rawToken is null || rawToken.Length != SessionPolicy.RefreshTokenLength)
            return false;

        Span<byte> raw = stackalloc byte[TokenBytes];
        try
        {
            // Base64Url.TryDecodeFromChars throws on illegal characters, so check validity first.
            if (!Base64Url.IsValid(rawToken, out var decodedLength) || decodedLength != TokenBytes)
                return false;

            if (!Base64Url.TryDecodeFromChars(rawToken, raw, out var written) || written != TokenBytes)
                return false;

            // Canonical form only: reject encodings that differ in the unused trailing bits.
            if (!string.Equals(Base64Url.EncodeToString(raw), rawToken, StringComparison.Ordinal))
                return false;

            hash = SHA256.HashData(raw);
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }
}
