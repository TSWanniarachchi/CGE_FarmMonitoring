using Microsoft.Extensions.Options;

/*
 * Description  : JWT settings ("Jwt" section) and their startup validation (Step 0 spec sections 7.2, 9; D-23).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Security;

/// <summary>
/// JWT settings. <see cref="SigningKey"/> (base64, at least 32 bytes decoded) is a secret held in user-secrets
/// (Development) or an environment variable and is never committed. Issuer / audience default to the approved values (D-23).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public const int MinSigningKeyBytes = 32;
    public const int MinLifetimeMinutes = 1;
    public const int MaxLifetimeMinutes = 30;
    public const int MaxClockSkewSeconds = 60;

    public string Issuer { get; set; } = "FarmFMS.API";

    public string Audience { get; set; } = "FarmFMS";

    /// <summary>Base64 of the HS256 signing key (at least 256 bits).</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime (D-09: 10 minutes). Range 1-30; it must not exceed the 30-minute idle timeout.</summary>
    public int AccessTokenLifetimeMinutes { get; set; } = 10;

    /// <summary>Tolerated clock difference when validating expiry (D-23: 30 seconds). Range 0-60.</summary>
    public int ClockSkewSeconds { get; set; } = 30;

    /// <summary>Decodes <see cref="SigningKey"/>; throws <see cref="InvalidOperationException"/> when it is invalid.</summary>
    internal byte[] GetSigningKeyBytes()
    {
        if (!JwtOptionsValidator.TryDecodeSigningKey(SigningKey, out var key))
            throw new InvalidOperationException($"Jwt:SigningKey must be base64 of at least {MinSigningKeyBytes} bytes.");

        return key;
    }
}

/// <summary>Validates <see cref="JwtOptions"/> at startup (AC-15). Messages never contain the key.</summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
            failures.Add("Jwt:Issuer is required.");
        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add("Jwt:Audience is required.");

        if (!JwtOptionsValidator.TryDecodeSigningKey(options.SigningKey, out _))
            failures.Add($"Jwt:SigningKey is required: base64 of at least {JwtOptions.MinSigningKeyBytes} bytes.");

        if (options.AccessTokenLifetimeMinutes is < JwtOptions.MinLifetimeMinutes or > JwtOptions.MaxLifetimeMinutes)
            failures.Add($"Jwt:AccessTokenLifetimeMinutes must be between {JwtOptions.MinLifetimeMinutes} and {JwtOptions.MaxLifetimeMinutes}.");

        if (options.ClockSkewSeconds is < 0 or > JwtOptions.MaxClockSkewSeconds)
            failures.Add($"Jwt:ClockSkewSeconds must be between 0 and {JwtOptions.MaxClockSkewSeconds}.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    internal static bool TryDecodeSigningKey(string? value, out byte[] key)
    {
        key = [];
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var decoded = Convert.FromBase64String(value.Trim());
            if (decoded.Length < JwtOptions.MinSigningKeyBytes)
                return false;

            key = decoded;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
