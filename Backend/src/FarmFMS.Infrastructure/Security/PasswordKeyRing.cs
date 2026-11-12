using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

/*
 * Description  : In-memory ring of password encryption keys (AD-11, design 4.3).
 * Author       : T.S.Wanniarachchi
 * Created Date : 12-11-2026
 */
namespace FarmFMS.Infrastructure.Security;

/// <summary>
/// Singleton holding the decoded keys as <c>byte[]</c>; they are zeroed on dispose. Lookup is exact (ordinal) and
/// never falls back to another version. The options are validated at startup
/// (<see cref="PasswordEncryptionOptionsValidator"/>); the constructor re-checks defensively.
/// </summary>
internal sealed class PasswordKeyRing : IDisposable
{
    private readonly Dictionary<string, byte[]> _keys = new(StringComparer.Ordinal);
    private bool _disposed;

    public PasswordKeyRing(IOptions<PasswordEncryptionOptions> options)
        : this(options?.Value ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    public PasswordKeyRing(PasswordEncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var validation = new PasswordEncryptionOptionsValidator().Validate(null, options);
        if (validation.Failed)
            throw new OptionsValidationException(
                PasswordEncryptionOptions.SectionName, typeof(PasswordEncryptionOptions), validation.Failures);

        foreach (var (version, value) in options.Keys)
        {
            PasswordEncryptionOptionsValidator.TryDecodeKey(value, out var key);
            _keys[version] = key;
        }

        ActiveKeyVersion = options.ActiveKeyVersion;
    }

    /// <summary>The version used for new payloads.</summary>
    public string ActiveKeyVersion { get; }

    /// <summary>The key of an exact version; false for unknown versions. The array must not be modified or kept.</summary>
    public bool TryGetKey(string? version, [NotNullWhen(true)] out byte[]? key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        key = null;
        return version is not null && _keys.TryGetValue(version, out key);
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var key in _keys.Values)
            CryptographicOperations.ZeroMemory(key);

        _keys.Clear();
        _disposed = true;
    }
}
