using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using FarmFMS.Application.Module01.Authentication;
using FarmFMS.Domain.Module01;

/*
 * Description  : AES-256-GCM password protector (Step 0 spec section 7.1, AD-11, design 2, 4.3, 6.2; P-02, P-03 Option A, P-23).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Security;

/// <summary>
/// Reversible password protection with <see cref="AesGcm"/> (256-bit key, 12-byte nonce, 16-byte tag).
/// <list type="bullet">
/// <item>Plaintext = UTF-8 of the NFC-normalized password (P-02).</item>
/// <item>AAD = <c>UTF8("FarmFMS.Password.v1") || 0x00 || UTF8(KeyVersion) || 0x00 || BigEndianInt32(userId)</c> (P-03 Option A, D-22).</item>
/// <item>Payload sizes are validated before decrypt (nonce 12, tag 16, ciphertext 1-512); the key version is looked up
/// exactly, never falling back to another key (P-23).</item>
/// <item>Comparison uses <see cref="CryptographicOperations.FixedTimeEquals"/>; plaintext buffers are zeroed.
/// Exceptions and messages never contain payload bytes or passwords.</item>
/// </list>
/// </summary>
internal sealed class AesGcmPasswordProtector : IPasswordProtector
{
    internal const string AadLabel = "FarmFMS.Password.v1";

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int MaxCiphertextSize = Module01FieldLengths.EncPassword;

    private readonly PasswordKeyRing _keyRing;

    public AesGcmPasswordProtector(PasswordKeyRing keyRing)
    {
        _keyRing = keyRing ?? throw new ArgumentNullException(nameof(keyRing));
    }

    public PasswordVerificationResult Verify(string password, EncryptedPassword payload, int userId)
    {
        ArgumentNullException.ThrowIfNull(payload);

        // 1. Structural validation before any decryption.
        if (payload.Nonce is not { Length: NonceSize } ||
            payload.Tag is not { Length: TagSize } ||
            payload.Ciphertext is not { Length: >= 1 and <= MaxCiphertextSize })
        {
            return PasswordVerificationResult.IntegrityFailure;
        }

        // 2. Exact key version, no fallback.
        if (!_keyRing.TryGetKey(payload.KeyVersion, out var key))
            return PasswordVerificationResult.UnknownKeyVersion;

        byte[]? candidate = null;
        byte[]? stored = null;
        try
        {
            // 3. Decrypt (GCM verifies the tag and the AAD binding to key version and user).
            var aad = BuildAad(payload.KeyVersion, userId);
            stored = new byte[payload.Ciphertext.Length];
            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(payload.Nonce, payload.Ciphertext, payload.Tag, stored, aad);
            }
            catch (CryptographicException)
            {
                // Includes AuthenticationTagMismatchException. The message is deliberately not propagated.
                return PasswordVerificationResult.IntegrityFailure;
            }

            // 4. Compare in constant time.
            if (!TryEncode(password, out candidate))
                return PasswordVerificationResult.Mismatch;

            return CryptographicOperations.FixedTimeEquals(stored, candidate)
                ? PasswordVerificationResult.Match
                : PasswordVerificationResult.Mismatch;
        }
        finally
        {
            if (stored is not null) CryptographicOperations.ZeroMemory(stored);
            if (candidate is not null) CryptographicOperations.ZeroMemory(candidate);
        }
    }

    public EncryptedPassword Protect(string password, int userId)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (!TryEncode(password, out var plaintext))
            throw new ArgumentException("The password is not valid Unicode.", nameof(password));

        try
        {
            if (plaintext.Length is 0 or > MaxCiphertextSize)
                throw new ArgumentException(
                    $"The password must be between 1 and {MaxCiphertextSize} UTF-8 bytes.", nameof(password));

            var version = _keyRing.ActiveKeyVersion;
            if (!_keyRing.TryGetKey(version, out var key))
                throw new InvalidOperationException("The active password key version is not in the key ring.");

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSize];

            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, BuildAad(version, userId));

            return new EncryptedPassword(ciphertext, nonce, tag, version);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public EncryptedPassword CreatePlaceholder() => new(
        RandomNumberGenerator.GetBytes(32),
        RandomNumberGenerator.GetBytes(NonceSize),
        RandomNumberGenerator.GetBytes(TagSize),
        _keyRing.ActiveKeyVersion);

    /// <summary>The associated data: label, key version and user Id, separated by 0x00 (D-22).</summary>
    internal static byte[] BuildAad(string keyVersion, int userId)
    {
        var label = Encoding.UTF8.GetBytes(AadLabel);
        var version = Encoding.UTF8.GetBytes(keyVersion);

        var aad = new byte[label.Length + 1 + version.Length + 1 + sizeof(int)];
        label.CopyTo(aad, 0);
        // aad[label.Length] = 0x00 (default)
        version.CopyTo(aad, label.Length + 1);
        // aad[label.Length + 1 + version.Length] = 0x00 (default)
        BinaryPrimitives.WriteInt32BigEndian(aad.AsSpan(label.Length + 1 + version.Length + 1), userId);

        return aad;
    }

    /// <summary>UTF-8 of the NFC-normalized password (P-02); false when the text is not valid Unicode.</summary>
    private static bool TryEncode(string password, out byte[] bytes)
    {
        try
        {
            bytes = Encoding.UTF8.GetBytes(password.Normalize(NormalizationForm.FormC));
            return true;
        }
        catch (ArgumentException)
        {
            bytes = [];
            return false;
        }
    }
}
