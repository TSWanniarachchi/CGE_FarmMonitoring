/*
 * Description  : Password protection abstraction (AES-256-GCM implemented in Infrastructure, AD-11, AD-02).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Application.Module01.Authentication;

/// <summary>
/// Verifies and protects passwords. The Application layer never sees keys or algorithms (AD-02).
/// </summary>
public interface IPasswordProtector
{
    /// <summary>
    /// Checks <paramref name="password"/> against the stored <paramref name="payload"/> of user <paramref name="userId"/>
    /// (the payload is bound to the user Id, P-03 Option A). Never throws for bad stored data.
    /// </summary>
    PasswordVerificationResult Verify(string password, EncryptedPassword payload, int userId);

    /// <summary>
    /// Encrypts <paramref name="password"/> for user <paramref name="userId"/> with the active key version.
    /// Step 0: bootstrap seed only. Throws <see cref="ArgumentException"/> when the password is empty or longer than 512 UTF-8 bytes.
    /// </summary>
    EncryptedPassword Protect(string password, int userId);

    /// <summary>
    /// A random payload of the correct sizes (CSPRNG bytes, active key version) that never verifies. Used only to
    /// satisfy NOT NULL columns before the real payload can be created (the user Id is the AAD, so it must exist first).
    /// </summary>
    EncryptedPassword CreatePlaceholder();
}

/// <summary>Outcome of <see cref="IPasswordProtector.Verify"/>.</summary>
public enum PasswordVerificationResult
{
    /// <summary>The password matches.</summary>
    Match = 0,

    /// <summary>The payload is intact and the password differs.</summary>
    Mismatch = 1,

    /// <summary>Tag mismatch, tampering, wrong binding or invalid lengths (P-23: not counted as a failed attempt).</summary>
    IntegrityFailure = 2,

    /// <summary><c>KeyVersion</c> is not in the key ring. No other key is tried (P-23: not counted).</summary>
    UnknownKeyVersion = 3
}

/// <summary>The encrypted password columns of <c>AppUser</c>.</summary>
public sealed record EncryptedPassword(byte[] Ciphertext, byte[] Nonce, byte[] Tag, string KeyVersion);
