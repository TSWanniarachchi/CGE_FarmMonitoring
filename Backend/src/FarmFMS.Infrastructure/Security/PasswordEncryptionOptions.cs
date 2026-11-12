using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

/*
 * Description  : Password key ring settings ("PasswordEncryption" section) and their startup validation (D-22, design 4.3).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Security;

/// <summary>
/// Key ring configuration. The key values live in user-secrets (Development, P-08) or environment variables and are
/// never committed: <c>PasswordEncryption:ActiveKeyVersion</c> and <c>PasswordEncryption:Keys:{version}</c> (base64, 32 bytes).
/// </summary>
public sealed class PasswordEncryptionOptions
{
    public const string SectionName = "PasswordEncryption";

    /// <summary>Version label used for new payloads; must exist in <see cref="Keys"/>.</summary>
    public string ActiveKeyVersion { get; set; } = string.Empty;

    /// <summary>Key version label to base64 of a 256-bit key.</summary>
    public Dictionary<string, string> Keys { get; set; } = [];
}

/// <summary>
/// Validates <see cref="PasswordEncryptionOptions"/> at startup: active version present, every key valid base64 of
/// exactly 32 bytes, version label <c>^[A-Za-z0-9-]{1,50}$</c> (D-22), no duplicate labels (case-insensitive).
/// Messages never contain key material.
/// </summary>
public sealed partial class PasswordEncryptionOptionsValidator : IValidateOptions<PasswordEncryptionOptions>
{
    public const int KeySizeBytes = 32;

    public ValidateOptionsResult Validate(string? name, PasswordEncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ActiveKeyVersion))
            failures.Add("PasswordEncryption:ActiveKeyVersion is required.");
        else if (!IsValidVersionLabel(options.ActiveKeyVersion))
            failures.Add("PasswordEncryption:ActiveKeyVersion must match ^[A-Za-z0-9-]{1,50}$.");

        var keys = options.Keys ?? [];
        if (keys.Count == 0)
            failures.Add("PasswordEncryption:Keys must contain at least one key.");

        var duplicates = keys.Keys
            .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (var duplicate in duplicates)
            failures.Add($"PasswordEncryption:Keys contains the version '{duplicate}' more than once (labels are case-insensitive).");

        foreach (var (version, value) in keys)
        {
            if (!IsValidVersionLabel(version))
            {
                failures.Add("PasswordEncryption:Keys contains a version label that does not match ^[A-Za-z0-9-]{1,50}$.");
                continue;
            }

            if (!TryDecodeKey(value, out var key))
            {
                failures.Add($"PasswordEncryption:Keys:{version} must be base64 of exactly {KeySizeBytes} bytes.");
                continue;
            }

            System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        }

        if (!string.IsNullOrWhiteSpace(options.ActiveKeyVersion) && !keys.ContainsKey(options.ActiveKeyVersion))
            failures.Add("PasswordEncryption:ActiveKeyVersion does not exist in PasswordEncryption:Keys.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    internal static bool IsValidVersionLabel(string? label) =>
        label is not null && VersionLabelRegex().IsMatch(label);

    /// <summary>Decodes a base64 key; true only when it is exactly <see cref="KeySizeBytes"/> bytes.</summary>
    internal static bool TryDecodeKey(string? value, out byte[] key)
    {
        key = [];
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var decoded = Convert.FromBase64String(value.Trim());
            if (decoded.Length != KeySizeBytes)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(decoded);
                return false;
            }

            key = decoded;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^[A-Za-z0-9-]{1,50}\z")]
    private static partial Regex VersionLabelRegex();
}
